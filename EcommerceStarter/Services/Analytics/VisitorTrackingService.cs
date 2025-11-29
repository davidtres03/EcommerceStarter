using EcommerceStarter.Data;
using EcommerceStarter.Services;
using EcommerceStarter.Models.VisitorTracking;
using Microsoft.EntityFrameworkCore;
using UAParser;

namespace EcommerceStarter.Services.Analytics
{
    /// <summary>
    /// Service for tracking visitor analytics
    /// </summary>
    public class VisitorTrackingService : IVisitorTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisitorTrackingService> _logger;
        private readonly IQueuedEventService? _queuedEventService;
        private readonly IUserAgentParserService _userAgentParser;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IAnalyticsExclusionMetricsService? _exclusionMetrics;
        private const string SessionCookieName = "visitor_session_id";
        private static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(30);

        public VisitorTrackingService(
            ApplicationDbContext context,
            ILogger<VisitorTrackingService> logger,
            IUserAgentParserService userAgentParser,
            ISecuritySettingsService securitySettingsService,
            IQueuedEventService? queuedEventService = null,
            IAnalyticsExclusionMetricsService? exclusionMetrics = null)
        {
            _context = context;
            _logger = logger;
            _userAgentParser = userAgentParser;
            _securitySettingsService = securitySettingsService;
            _queuedEventService = queuedEventService;
            _exclusionMetrics = exclusionMetrics;
        }

        /// <summary>
        /// Get or create a visitor session
        /// IMPORTANT: Whitelisted IPs do NOT create sessions or track analytics
        /// </summary>
        public async Task<VisitorSession> GetOrCreateSessionAsync(HttpContext context)
        {
            try
            {
                // Get client IP for whitelist check
                var clientIp = GetClientIpAddress(context);
                
                // Check if IP is whitelisted FIRST - whitelisted IPs don't get tracked at all
                var ignoreReason = await _securitySettingsService.GetAnalyticsIgnoreReasonAsync(clientIp);
                if (ignoreReason != AnalyticsIgnoreReason.None)
                {
                    if (ignoreReason == AnalyticsIgnoreReason.Whitelisted)
                    {
                        _logger.LogDebug("Whitelisted IP {IpAddress} - skipping analytics tracking entirely", clientIp);
                        _exclusionMetrics?.RecordWhitelistExclusion();
                    }
                    else if (ignoreReason == AnalyticsIgnoreReason.PrivateOrLocal)
                    {
                        _logger.LogDebug("Private/Local IP {IpAddress} - skipping analytics tracking", clientIp);
                        _exclusionMetrics?.RecordPrivateOrLocalExclusion();
                    }
                    
                    // Return a temporary in-memory session (not saved to database)
                    // This prevents null reference errors in calling code
                    return new VisitorSession
                    {
                        SessionId = $"whitelist-{Guid.NewGuid()}",
                        IpAddress = clientIp,
                        StartTime = DateTime.UtcNow,
                        LastActivityTime = DateTime.UtcNow
                    };
                }

                // Check for existing session cookie
                string? sessionId = context.Request.Cookies[SessionCookieName];
                VisitorSession? session = null;

                if (!string.IsNullOrEmpty(sessionId))
                {
                    // Try to find existing session
                    session = await _context.VisitorSessions
                        .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.EndTime == null);

                    // Check if session has timed out
                    if (session != null && DateTime.UtcNow - session.LastActivityTime > SessionTimeout)
                    {
                        session.EndTime = session.LastActivityTime;
                        session = null; // Create new session
                    }
                }

                // Create new session if none exists or timed out
                if (session == null)
                {
                    session = new VisitorSession
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        IpAddress = AnonymizeIpAddress(GetClientIpAddress(context)),
                        UserAgent = context.Request.Headers["User-Agent"].ToString(),
                        Referrer = context.Request.Headers["Referer"].ToString(),
                        LandingPage = context.Request.Path + context.Request.QueryString,
                        StartTime = DateTime.UtcNow,
                        LastActivityTime = DateTime.UtcNow
                    };

                    // Parse user agent for device info using enhanced parser
                    ParseUserAgent(session, context.Request.Headers);

                    _context.VisitorSessions.Add(session);
                    await _context.SaveChangesAsync();

                    // Queue geolocation lookup (processed by Windows Service)
                    // Note: Whitelisted IPs already filtered out above, so this will always queue
                    if (_queuedEventService != null && !string.IsNullOrEmpty(session.IpAddress))
                    {
                        _queuedEventService.QueueGeolocation(session.Id, session.IpAddress);
                        _logger.LogDebug("Queued geolocation lookup for session {SessionId}, IP {IpAddress}", 
                            session.SessionId, session.IpAddress);
                    }

                    // Get user ID if authenticated
                    if (context.User?.Identity?.IsAuthenticated == true)
                    {
                        session.UserId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        await _context.SaveChangesAsync();
                    }

                    // Set session cookie (30 minutes)
                    context.Response.Cookies.Append(SessionCookieName, session.SessionId, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                    });
                }
                else
                {
                    // Update last activity time only if it's been more than 2 minutes (debouncing)
                    if (DateTime.UtcNow - session.LastActivityTime > TimeSpan.FromMinutes(2))
                    {
                        session.LastActivityTime = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating visitor session");
                throw;
            }
        }

        /// <summary>
        /// Track a page view
        /// </summary>
        public async Task TrackPageViewAsync(int sessionId, string url, string? pageTitle, string? referrer)
        {
            try
            {
                // Skip tracking for admin pages - keep analytics customer-focused
                if (url?.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogDebug("Skipping analytics tracking for admin page: {Url}", url);
                    _exclusionMetrics?.RecordAdminPageExclusion();
                    return;
                }

                // If queue service available, use it for non-blocking writes
                if (_queuedEventService != null)
                {
                    _queuedEventService.QueuePageView(sessionId, url, pageTitle, referrer);
                    return;
                }

                // Fallback: direct write if queue service not available
                // CRITICAL: Verify session exists before adding page view
                var session = await _context.VisitorSessions.FindAsync(sessionId);
                if (session == null)
                {
                    _logger.LogWarning("Cannot track page view - session {SessionId} does not exist", sessionId);
                    return;
                }

                var pageView = new PageView
                {
                    SessionId = sessionId,
                    Url = url,
                    PageTitle = pageTitle,
                    Referrer = referrer,
                    Timestamp = DateTime.UtcNow
                };

                _context.PageViews.Add(pageView);

                // Update session page view count
                session.PageViewCount++;
                session.LastActivityTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking page view for session {SessionId}: {Url}", sessionId, url);
            }
        }

        /// <summary>
        /// Track a custom event
        /// </summary>
        public async Task TrackEventAsync(int sessionId, string category, string action, string? label = null, decimal? value = null, string? metadata = null)
        {
            try
            {
                // If queue service available, use it for non-blocking writes
                if (_queuedEventService != null)
                {
                    _queuedEventService.QueueVisitorEvent(sessionId, category, action, label, value, metadata);
                    return;
                }

                // Fallback: direct write if queue service not available
                var visitorEvent = new VisitorEvent
                {
                    SessionId = sessionId,
                    Category = category,
                    Action = action,
                    Label = label,
                    Value = value,
                    Metadata = metadata,
                    Timestamp = DateTime.UtcNow
                };

                _context.VisitorEvents.Add(visitorEvent);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking event for session {SessionId}: {Category}/{Action}", sessionId, category, action);
            }
        }

        /// <summary>
        /// Mark session as converted
        /// </summary>
        public async Task MarkSessionAsConvertedAsync(int sessionId)
        {
            try
            {
                var session = await _context.VisitorSessions.FindAsync(sessionId);
                if (session != null)
                {
                    session.Converted = true;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking session {SessionId} as converted", sessionId);
            }
        }

        /// <summary>
        /// Get analytics summary
        /// </summary>
        public async Task<AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var sessions = await _context.VisitorSessions
                    .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
                    .ToListAsync();

                var totalPageViews = await _context.PageViews
                    .Where(pv => pv.Timestamp >= startDate && pv.Timestamp <= endDate)
                    .CountAsync();

                var uniqueVisitors = sessions
                    .GroupBy(s => s.IpAddress)
                    .Count();

                var convertedSessions = sessions.Count(s => s.Converted);
                var sessionsWithOnePageView = sessions.Count(s => s.PageViewCount == 1);

                // Calculate average session duration - handle active sessions
                var completedSessions = sessions.Where(s => s.EndTime.HasValue).ToList();
                var averageSessionDuration = completedSessions.Any()
                    ? completedSessions.Average(s => (s.EndTime!.Value - s.StartTime).TotalSeconds)
                    : 0;

                return new AnalyticsSummary
                {
                    TotalSessions = sessions.Count,
                    TotalPageViews = totalPageViews,
                    UniqueVisitors = uniqueVisitors,
                    ConvertedSessions = convertedSessions,
                    ConversionRate = sessions.Count > 0 ? (decimal)convertedSessions / sessions.Count * 100 : 0,
                    AverageSessionDuration = averageSessionDuration,
                    AveragePagesPerSession = sessions.Count > 0 ? (double)totalPageViews / sessions.Count : 0,
                    BounceRate = sessions.Count > 0 ? sessionsWithOnePageView * 100 / sessions.Count : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analytics summary");
                // Return empty summary instead of throwing
                return new AnalyticsSummary();
            }
        }

        /// <summary>
        /// Get top pages by views
        /// </summary>
        public async Task<List<PageStatistic>> GetTopPagesAsync(DateTime startDate, DateTime endDate, int count = 10)
        {
            try
            {
                var pageStats = await _context.PageViews
                    .Where(pv => pv.Timestamp >= startDate && pv.Timestamp <= endDate)
                    .GroupBy(pv => new { pv.Url, pv.PageTitle })
                    .Select(g => new PageStatistic
                    {
                        Url = g.Key.Url,
                        PageTitle = g.Key.PageTitle,
                        Views = g.Count(),
                        UniqueVisitors = g.Select(pv => pv.Session.IpAddress).Distinct().Count(),
                        AverageTimeOnPage = g.Average(pv => pv.TimeOnPage ?? 0)
                    })
                    .OrderByDescending(ps => ps.Views)
                    .Take(count)
                    .ToListAsync();

                return pageStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top pages");
                return new List<PageStatistic>();
            }
        }

        /// <summary>
        /// Get top referrers
        /// </summary>
        public async Task<List<ReferrerStatistic>> GetTopReferrersAsync(DateTime startDate, DateTime endDate, int count = 10)
        {
            try
            {
                var referrerStats = await _context.VisitorSessions
                    .Where(s => s.StartTime >= startDate && s.StartTime <= endDate && !string.IsNullOrEmpty(s.Referrer))
                    .GroupBy(s => s.Referrer)
                    .Select(g => new ReferrerStatistic
                    {
                        Referrer = g.Key ?? "Direct",
                        Sessions = g.Count(),
                        Conversions = g.Count(s => s.Converted)
                    })
                    .OrderByDescending(rs => rs.Sessions)
                    .Take(count)
                    .ToListAsync();

                return referrerStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top referrers");
                return new List<ReferrerStatistic>();
            }
        }

        /// <summary>
        /// Get device type breakdown
        /// </summary>
        public async Task<Dictionary<string, int>> GetDeviceTypeBreakdownAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var deviceBreakdown = await _context.VisitorSessions
                    .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
                    .GroupBy(s => s.DeviceType ?? "Unknown")
                    .Select(g => new { DeviceType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.DeviceType, x => x.Count);

                return deviceBreakdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device type breakdown");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Get browser breakdown
        /// </summary>
        public async Task<Dictionary<string, int>> GetBrowserBreakdownAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var browserBreakdown = await _context.VisitorSessions
                    .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
                    .GroupBy(s => s.Browser ?? "Unknown")
                    .Select(g => new { Browser = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Browser, x => x.Count);

                return browserBreakdown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting browser breakdown");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Get recent events
        /// </summary>
        public async Task<List<VisitorEvent>> GetRecentEventsAsync(int count = 50)
        {
            try
            {
                return await _context.VisitorEvents
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent events");
                return new List<VisitorEvent>();
            }
        }

        public async Task<List<VisitorSession>> GetRecentSessionsAsync(int count = 50)
        {
            try
            {
                return await _context.VisitorSessions
                    .OrderByDescending(s => s.StartTime)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sessions");
                return new List<VisitorSession>();
            }
        }

        #region Helper Methods

        /// <summary>
        /// Get client IP address from request
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP (behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Anonymize IP address for privacy (GDPR compliance)
        /// </summary>
        private string AnonymizeIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "Unknown")
                return ipAddress;

            // For IPv4: mask last octet
            if (ipAddress.Contains('.'))
            {
                var parts = ipAddress.Split('.');
                if (parts.Length == 4)
                {
                    return $"{parts[0]}.{parts[1]}.{parts[2]}.0";
                }
            }

            // For IPv6: mask last 80 bits
            if (ipAddress.Contains(':'))
            {
                var parts = ipAddress.Split(':');
                if (parts.Length >= 4)
                {
                    return $"{parts[0]}:{parts[1]}:{parts[2]}:{parts[3]}::";
                }
            }

            return ipAddress;
        }

        /// <summary>
        /// Parse user agent for device information using enhanced parser
        /// </summary>
        private void ParseUserAgent(VisitorSession session, IHeaderDictionary headers)
        {
            if (string.IsNullOrEmpty(session.UserAgent))
                return;

            try
            {
                var userAgentInfo = _userAgentParser.Parse(session.UserAgent, headers);

                session.Browser = userAgentInfo.Browser;
                session.BrowserVersion = userAgentInfo.BrowserVersion;
                session.OperatingSystem = userAgentInfo.OperatingSystem;
                session.OSVersion = userAgentInfo.OSVersion;
                session.DeviceType = userAgentInfo.DeviceType;
                session.DeviceBrand = userAgentInfo.DeviceBrand;
                session.DeviceModel = userAgentInfo.DeviceModel;
                session.IsBot = userAgentInfo.IsBot;
                session.BotName = userAgentInfo.BotName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing user agent: {UserAgent}", session.UserAgent);
                session.Browser = "Unknown";
                session.DeviceType = "Unknown";
                session.OperatingSystem = "Unknown";
            }
        }

        #endregion
    }
}
