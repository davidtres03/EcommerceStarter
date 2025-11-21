using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models.VisitorTracking;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsApiController> _logger;

        public AnalyticsApiController(ApplicationDbContext context, ILogger<AnalyticsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/analytics/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetAnalyticsSummary(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Default to last 30 days if no dates provided
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Ensure start is before end
                if (start > end)
                {
                    return BadRequest(new { success = false, message = "Start date must be before end date" });
                }

                // Get sessions in date range
                var sessions = await _context.VisitorSessions
                    .Where(s => s.StartTime >= start && s.StartTime <= end)
                    .ToListAsync();

                // Get page views in date range
                var pageViews = await _context.PageViews
                    .Where(pv => pv.Timestamp >= start && pv.Timestamp <= end)
                    .ToListAsync();

                // Get events in date range
                var events = await _context.VisitorEvents
                    .Where(e => e.Timestamp >= start && e.Timestamp <= end)
                    .ToListAsync();

                // Calculate metrics
                var totalSessions = sessions.Count;
                var totalPageViews = pageViews.Count;
                var totalEvents = events.Count;
                var uniqueVisitors = sessions.Select(s => s.IpAddress).Distinct().Count();
                var conversions = sessions.Count(s => s.Converted);
                var conversionRate = totalSessions > 0 ? (decimal)conversions / totalSessions * 100 : 0;

                // Average session duration (in minutes)
                var completedSessions = sessions.Where(s => s.EndTime.HasValue).ToList();
                var avgSessionDuration = completedSessions.Any()
                    ? completedSessions.Average(s => (s.EndTime!.Value - s.StartTime).TotalMinutes)
                    : 0;

                // Average pages per session
                var avgPagesPerSession = totalSessions > 0 ? (decimal)totalPageViews / totalSessions : 0;

                // Bounce rate (sessions with only 1 page view)
                var bouncedSessions = sessions.Count(s => s.PageViewCount == 1);
                var bounceRate = totalSessions > 0 ? (decimal)bouncedSessions / totalSessions * 100 : 0;

                // Daily breakdown
                var dailyStats = sessions
                    .GroupBy(s => s.StartTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Sessions = g.Count(),
                        PageViews = pageViews.Count(pv => pv.Timestamp.Date == g.Key),
                        UniqueVisitors = g.Select(s => s.IpAddress).Distinct().Count(),
                        Conversions = g.Count(s => s.Converted)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        dateRange = new { start, end },
                        summary = new
                        {
                            totalSessions,
                            totalPageViews,
                            totalEvents,
                            uniqueVisitors,
                            conversions,
                            conversionRate = Math.Round(conversionRate, 2),
                            avgSessionDuration = Math.Round(avgSessionDuration, 2),
                            avgPagesPerSession = Math.Round(avgPagesPerSession, 2),
                            bounceRate = Math.Round(bounceRate, 2)
                        },
                        dailyBreakdown = dailyStats
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics summary");
                return StatusCode(500, new { success = false, message = "Error retrieving analytics summary" });
            }
        }

        // GET: api/analytics/top-pages
        [HttpGet("top-pages")]
        public async Task<IActionResult> GetTopPages(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start > end)
                {
                    return BadRequest(new { success = false, message = "Start date must be before end date" });
                }

                var topPages = await _context.PageViews
                    .Where(pv => pv.Timestamp >= start && pv.Timestamp <= end)
                    .GroupBy(pv => new { pv.Url, pv.PageTitle })
                    .Select(g => new
                    {
                        Url = g.Key.Url,
                        PageTitle = g.Key.PageTitle ?? "Untitled",
                        Views = g.Count(),
                        UniqueVisitors = g.Select(pv => pv.Session.IpAddress).Distinct().Count(),
                        AvgTimeOnPage = g.Where(pv => pv.TimeOnPage.HasValue).Any()
                            ? g.Where(pv => pv.TimeOnPage.HasValue).Average(pv => pv.TimeOnPage!.Value)
                            : (double?)null
                    })
                    .OrderByDescending(p => p.Views)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = topPages.Select(p => new
                    {
                        p.Url,
                        p.PageTitle,
                        p.Views,
                        p.UniqueVisitors,
                        AvgTimeOnPage = p.AvgTimeOnPage.HasValue ? Math.Round(p.AvgTimeOnPage.Value, 1) : (double?)null
                    }),
                    dateRange = new { start, end },
                    count = topPages.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top pages");
                return StatusCode(500, new { success = false, message = "Error retrieving top pages" });
            }
        }

        // GET: api/analytics/referrers
        [HttpGet("referrers")]
        public async Task<IActionResult> GetTopReferrers(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start > end)
                {
                    return BadRequest(new { success = false, message = "Start date must be before end date" });
                }

                var topReferrers = await _context.VisitorSessions
                    .Where(s => s.StartTime >= start && s.StartTime <= end && !string.IsNullOrEmpty(s.Referrer))
                    .GroupBy(s => s.Referrer)
                    .Select(g => new
                    {
                        Referrer = g.Key,
                        Sessions = g.Count(),
                        UniqueVisitors = g.Select(s => s.IpAddress).Distinct().Count(),
                        Conversions = g.Count(s => s.Converted),
                        ConversionRate = g.Count() > 0 ? (decimal)g.Count(s => s.Converted) / g.Count() * 100 : 0
                    })
                    .OrderByDescending(r => r.Sessions)
                    .Take(limit)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = topReferrers.Select(r => new
                    {
                        r.Referrer,
                        r.Sessions,
                        r.UniqueVisitors,
                        r.Conversions,
                        ConversionRate = Math.Round(r.ConversionRate, 2)
                    }),
                    dateRange = new { start, end },
                    count = topReferrers.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top referrers");
                return StatusCode(500, new { success = false, message = "Error retrieving top referrers" });
            }
        }

        // GET: api/analytics/devices
        [HttpGet("devices")]
        public async Task<IActionResult> GetDeviceBreakdown(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start > end)
                {
                    return BadRequest(new { success = false, message = "Start date must be before end date" });
                }

                var deviceStats = await _context.VisitorSessions
                    .Where(s => s.StartTime >= start && s.StartTime <= end)
                    .GroupBy(s => s.DeviceType ?? "Unknown")
                    .Select(g => new
                    {
                        DeviceType = g.Key,
                        Sessions = g.Count(),
                        UniqueVisitors = g.Select(s => s.IpAddress).Distinct().Count(),
                        Conversions = g.Count(s => s.Converted)
                    })
                    .ToListAsync();

                // Calculate average session duration in memory to avoid nullable issues
                var deviceStatsWithDuration = deviceStats.Select(d => new
                {
                    d.DeviceType,
                    d.Sessions,
                    d.UniqueVisitors,
                    d.Conversions,
                    AvgSessionDuration = _context.VisitorSessions
                        .Where(s => s.StartTime >= start && s.StartTime <= end && 
                                    (s.DeviceType ?? "Unknown") == d.DeviceType && 
                                    s.EndTime.HasValue)
                        .AsEnumerable()
                        .Any()
                        ? _context.VisitorSessions
                            .Where(s => s.StartTime >= start && s.StartTime <= end && 
                                        (s.DeviceType ?? "Unknown") == d.DeviceType && 
                                        s.EndTime.HasValue)
                            .AsEnumerable()
                            .Average(s => (s.EndTime!.Value - s.StartTime).TotalMinutes)
                        : (double?)null
                }).OrderByDescending(d => d.Sessions).ToList();

                var totalSessions = deviceStatsWithDuration.Sum(d => d.Sessions);

                return Ok(new
                {
                    success = true,
                    data = deviceStatsWithDuration.Select(d => new
                    {
                        d.DeviceType,
                        d.Sessions,
                        Percentage = totalSessions > 0 ? Math.Round((decimal)d.Sessions / totalSessions * 100, 2) : 0,
                        d.UniqueVisitors,
                        d.Conversions,
                        AvgSessionDuration = d.AvgSessionDuration.HasValue ? Math.Round(d.AvgSessionDuration.Value, 2) : (double?)null
                    }),
                    dateRange = new { start, end },
                    count = deviceStatsWithDuration.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device breakdown");
                return StatusCode(500, new { success = false, message = "Error retrieving device breakdown" });
            }
        }

        // GET: api/analytics/browsers
        [HttpGet("browsers")]
        public async Task<IActionResult> GetBrowserBreakdown(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                if (start > end)
                {
                    return BadRequest(new { success = false, message = "Start date must be before end date" });
                }

                var browserStats = await _context.VisitorSessions
                    .Where(s => s.StartTime >= start && s.StartTime <= end)
                    .GroupBy(s => s.Browser ?? "Unknown")
                    .Select(g => new
                    {
                        Browser = g.Key,
                        Sessions = g.Count(),
                        UniqueVisitors = g.Select(s => s.IpAddress).Distinct().Count(),
                        Conversions = g.Count(s => s.Converted)
                    })
                    .OrderByDescending(b => b.Sessions)
                    .ToListAsync();

                var totalSessions = browserStats.Sum(b => b.Sessions);

                return Ok(new
                {
                    success = true,
                    data = browserStats.Select(b => new
                    {
                        b.Browser,
                        b.Sessions,
                        Percentage = totalSessions > 0 ? Math.Round((decimal)b.Sessions / totalSessions * 100, 2) : 0,
                        b.UniqueVisitors,
                        b.Conversions
                    }),
                    dateRange = new { start, end },
                    count = browserStats.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving browser breakdown");
                return StatusCode(500, new { success = false, message = "Error retrieving browser breakdown" });
            }
        }

        // GET: api/analytics/events
        [HttpGet("events")]
        public async Task<IActionResult> GetRecentEvents(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? category = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var start = startDate ?? DateTime.UtcNow.AddDays(-7);
                var end = endDate ?? DateTime.UtcNow;

                if (start > end)
                {
                    return BadRequest(new { success = false, message = "Start date must be before end date" });
                }

                var query = _context.VisitorEvents
                    .Where(e => e.Timestamp >= start && e.Timestamp <= end);

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(e => e.Category == category);
                }

                var totalCount = await query.CountAsync();

                var events = await query
                    .OrderByDescending(e => e.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        e.Id,
                        e.Category,
                        e.Action,
                        e.Label,
                        e.Value,
                        e.PageUrl,
                        e.Timestamp,
                        SessionId = e.Session.SessionId
                    })
                    .ToListAsync();

                // Get event categories breakdown
                var categoryBreakdown = await _context.VisitorEvents
                    .Where(e => e.Timestamp >= start && e.Timestamp <= end)
                    .GroupBy(e => e.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(c => c.Count)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = events,
                    categoryBreakdown,
                    totalCount,
                    page,
                    pageSize,
                    dateRange = new { start, end }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(500, new { success = false, message = "Error retrieving events" });
            }
        }

        // GET: api/analytics/real-time
        [HttpGet("real-time")]
        public async Task<IActionResult> GetRealTimeAnalytics()
        {
            try
            {
                // Get sessions from last 5 minutes
                var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

                var activeSessions = await _context.VisitorSessions
                    .Where(s => s.LastActivityTime >= fiveMinutesAgo && s.EndTime == null)
                    .CountAsync();

                var recentPageViews = await _context.PageViews
                    .Where(pv => pv.Timestamp >= fiveMinutesAgo)
                    .GroupBy(pv => pv.Url)
                    .Select(g => new
                    {
                        Url = g.Key,
                        Views = g.Count()
                    })
                    .OrderByDescending(p => p.Views)
                    .Take(5)
                    .ToListAsync();

                var recentEvents = await _context.VisitorEvents
                    .Where(e => e.Timestamp >= fiveMinutesAgo)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10)
                    .Select(e => new
                    {
                        e.Category,
                        e.Action,
                        e.Label,
                        e.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        activeSessions,
                        recentPageViews,
                        recentEvents,
                        asOf = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving real-time analytics");
                return StatusCode(500, new { success = false, message = "Error retrieving real-time analytics" });
            }
        }
    }
}
