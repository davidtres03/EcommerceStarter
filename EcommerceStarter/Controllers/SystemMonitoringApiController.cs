using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models.Service;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/admin/service")]
    public class SystemMonitoringApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemMonitoringApiController> _logger;
        private readonly ITimezoneService _timezoneService;

        public SystemMonitoringApiController(ApplicationDbContext context, ILogger<SystemMonitoringApiController> logger, ITimezoneService timezoneService)
        {
            _context = context;
            _logger = logger;
            _timezoneService = timezoneService;
        }

        // POST: api/admin/service/status/log
        [HttpPost("status/log")]
        [AllowAnonymous] // Allow Windows Service to log without authentication
        public async Task<IActionResult> LogServiceStatus([FromBody] ServiceStatusLogDto statusLog)
        {
            try
            {
                if (statusLog == null)
                {
                    return BadRequest(new { success = false, message = "Status log data is required" });
                }

                var newStatusLog = new ServiceStatusLog
                {
                    Timestamp = DateTime.UtcNow,
                    IsWebServiceOnline = statusLog.IsWebServiceOnline,
                    ResponseTimeMs = statusLog.ResponseTimeMs,
                    IsBackgroundServiceRunning = statusLog.IsBackgroundServiceRunning,
                    PendingOrdersCount = statusLog.PendingOrdersCount,
                    MemoryUsageMb = statusLog.MemoryUsageMb,
                    CpuUsagePercent = statusLog.CpuUsagePercent,
                    DatabaseConnected = statusLog.DatabaseConnected,
                    ActiveUserCount = statusLog.ActiveUserCount,
                    QueueSize = statusLog.QueueSize,
                    UptimePercent = statusLog.UptimePercent,
                    ErrorMessage = statusLog.ErrorMessage
                };

                _context.ServiceStatusLogs.Add(newStatusLog);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Service status logged successfully");

                return Ok(new { success = true, message = "Status logged successfully", id = newStatusLog.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging service status");
                return StatusCode(500, new { success = false, message = "Error logging service status" });
            }
        }

        // GET: api/admin/service/status
        [HttpGet("status")]
        public async Task<IActionResult> GetServiceStatus()
        {
            try
            {
                // Get the most recent status log
                var latestStatus = await _context.ServiceStatusLogs
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                // Get recent updates (last 5)
                var recentUpdates = await _context.UpdateHistories
                    .OrderByDescending(u => u.AppliedAt)
                    .Take(5)
                    .Select(u => new
                    {
                        u.Version,
                        AppliedAt = u.AppliedAt,
                        u.Status,
                        u.ReleaseNotes,
                        u.ApplyDurationSeconds
                    })
                    .ToListAsync();

                // Convert UTC timestamps to Central Time
                var recentUpdatesConverted = recentUpdates.Select(u => new
                {
                    u.Version,
                    AppliedAt = _timezoneService.ConvertUtcToLocalTime(u.AppliedAt),
                    u.Status,
                    u.ReleaseNotes,
                    u.ApplyDurationSeconds
                }).ToList();

                // Get unacknowledged errors (last 10)
                var recentErrors = await _context.ServiceErrorLogs
                    .Where(e => !e.IsAcknowledged)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10)
                    .Select(e => new
                    {
                        e.Id,
                        Timestamp = e.Timestamp,
                        e.Source,
                        e.Severity,
                        e.Message,
                        e.IsAcknowledged
                    })
                    .ToListAsync();

                // Convert UTC timestamps to Central Time
                var recentErrorsConverted = recentErrors.Select(e => new
                {
                    e.Id,
                    Timestamp = _timezoneService.ConvertUtcToLocalTime(e.Timestamp),
                    e.Source,
                    e.Severity,
                    e.Message,
                    e.IsAcknowledged
                }).ToList();

                if (latestStatus == null)
                {
                    // Return default values when no status logs exist
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            message = "No status logs available",
                            timestamp = _timezoneService.ConvertUtcToLocalTime(DateTime.UtcNow),
                            isWebServiceOnline = true,
                            responseTimeMs = 0,
                            isBackgroundServiceRunning = true,
                            pendingOrdersCount = 0,
                            memoryUsageMb = 0.0,
                            cpuUsagePercent = 0.0,
                            databaseConnected = true,
                            activeUserCount = 0,
                            queueSize = 0,
                            uptimePercent = 100.0,
                            errorMessage = (string?)null,
                            recentUpdates = recentUpdatesConverted,
                            recentErrors = recentErrorsConverted
                        }
                    });
                }

                var statusData = new
                {
                    timestamp = _timezoneService.ConvertUtcToLocalTime(latestStatus.Timestamp),
                    isWebServiceOnline = latestStatus.IsWebServiceOnline,
                    responseTimeMs = latestStatus.ResponseTimeMs,
                    isBackgroundServiceRunning = latestStatus.IsBackgroundServiceRunning,
                    pendingOrdersCount = latestStatus.PendingOrdersCount,
                    memoryUsageMb = latestStatus.MemoryUsageMb,
                    cpuUsagePercent = Math.Round(latestStatus.CpuUsagePercent, 2),
                    databaseConnected = latestStatus.DatabaseConnected,
                    activeUserCount = latestStatus.ActiveUserCount,
                    queueSize = latestStatus.QueueSize,
                    uptimePercent = Math.Round(latestStatus.UptimePercent, 2),
                    errorMessage = latestStatus.ErrorMessage,
                    recentUpdates = recentUpdatesConverted,
                    recentErrors = recentErrorsConverted
                };

                return Ok(new
                {
                    success = true,
                    data = statusData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service status");
                return StatusCode(500, new { success = false, message = "Error retrieving service status" });
            }
        }

        // GET: api/admin/service/errors
        [HttpGet("errors")]
        public async Task<IActionResult> GetServiceErrors(
            [FromQuery] string? severity = null,
            [FromQuery] string? source = null,
            [FromQuery] bool? acknowledged = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.ServiceErrorLogs.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(severity))
                {
                    query = query.Where(e => e.Severity == severity);
                }

                if (!string.IsNullOrWhiteSpace(source))
                {
                    query = query.Where(e => e.Source == source);
                }

                if (acknowledged.HasValue)
                {
                    query = query.Where(e => e.IsAcknowledged == acknowledged.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.Timestamp <= endDate.Value);
                }

                var totalCount = await query.CountAsync();

                var errors = await query
                    .OrderByDescending(e => e.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        e.Id,
                        Timestamp = e.Timestamp,
                        e.Source,
                        e.Severity,
                        e.Message,
                        e.StackTrace,
                        e.IsAcknowledged,
                        AcknowledgedAt = e.AcknowledgedAt
                    })
                    .ToListAsync();

                // Convert UTC timestamps to Central Time
                var errorsConverted = errors.Select(e => new
                {
                    e.Id,
                    Timestamp = _timezoneService.ConvertUtcToLocalTime(e.Timestamp),
                    e.Source,
                    e.Severity,
                    e.Message,
                    e.StackTrace,
                    e.IsAcknowledged,
                    AcknowledgedAt = _timezoneService.ConvertUtcToLocalTime(e.AcknowledgedAt)
                }).ToList();

                // Get available filter options
                var sources = await _context.ServiceErrorLogs
                    .Select(e => e.Source)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                var severities = await _context.ServiceErrorLogs
                    .Select(e => e.Severity)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = errorsConverted,
                    totalCount,
                    page,
                    pageSize,
                    filters = new
                    {
                        availableSources = sources,
                        availableSeverities = severities
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service errors");
                return StatusCode(500, new { success = false, message = "Error retrieving service errors" });
            }
        }

        // PUT: api/admin/service/errors/{id}/acknowledge
        [HttpPut("errors/{id}/acknowledge")]
        public async Task<IActionResult> AcknowledgeError(string id)
        {
            try
            {
                var error = await _context.ServiceErrorLogs.FindAsync(id);

                if (error == null)
                {
                    return NotFound(new { success = false, message = "Error log not found" });
                }

                if (error.IsAcknowledged)
                {
                    return BadRequest(new { success = false, message = "Error is already acknowledged" });
                }

                error.IsAcknowledged = true;
                error.AcknowledgedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Service error {ErrorId} acknowledged", id);

                return Ok(new
                {
                    success = true,
                    message = "Error acknowledged successfully",
                    data = new
                    {
                        error.Id,
                        error.IsAcknowledged,
                        AcknowledgedAt = _timezoneService.ConvertUtcToLocalTime(error.AcknowledgedAt)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging service error {ErrorId}", id);
                return StatusCode(500, new { success = false, message = "Error acknowledging service error" });
            }
        }

        // PUT: api/admin/service/errors/acknowledge-all
        [HttpPut("errors/acknowledge-all")]
        public async Task<IActionResult> AcknowledgeAllErrors([FromBody] AcknowledgeAllRequest? request = null)
        {
            try
            {
                var query = _context.ServiceErrorLogs.Where(e => !e.IsAcknowledged);

                // Apply optional filters
                if (request != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.Severity))
                    {
                        query = query.Where(e => e.Severity == request.Severity);
                    }

                    if (!string.IsNullOrWhiteSpace(request.Source))
                    {
                        query = query.Where(e => e.Source == request.Source);
                    }
                }

                var errors = await query.ToListAsync();

                if (!errors.Any())
                {
                    return Ok(new { success = true, message = "No unacknowledged errors found", count = 0 });
                }

                var acknowledgedAt = DateTime.UtcNow;
                foreach (var error in errors)
                {
                    error.IsAcknowledged = true;
                    error.AcknowledgedAt = acknowledgedAt;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Acknowledged {Count} service errors", errors.Count);

                return Ok(new
                {
                    success = true,
                    message = "All errors acknowledged successfully",
                    count = errors.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging all service errors");
                return StatusCode(500, new { success = false, message = "Error acknowledging all service errors" });
            }
        }

        // PUT: api/admin/service/errors/{id}/unacknowledge
        [HttpPut("errors/{id}/unacknowledge")]
        public async Task<IActionResult> UnacknowledgeError(string id)
        {
            try
            {
                var error = await _context.ServiceErrorLogs.FindAsync(id);

                if (error == null)
                {
                    return NotFound(new { success = false, message = "Error log not found" });
                }

                if (!error.IsAcknowledged)
                {
                    return BadRequest(new { success = false, message = "Error is not acknowledged" });
                }

                error.IsAcknowledged = false;
                error.AcknowledgedAt = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Service error {ErrorId} unacknowledged", id);

                return Ok(new
                {
                    success = true,
                    message = "Error unacknowledged successfully",
                    data = new
                    {
                        error.Id,
                        error.IsAcknowledged,
                        error.AcknowledgedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unacknowledging service error {ErrorId}", id);
                return StatusCode(500, new { success = false, message = "Error unacknowledging service error" });
            }
        }

        // GET: api/admin/service/updates
        [HttpGet("updates")]
        public async Task<IActionResult> GetUpdateHistory(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.UpdateHistories.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(u => u.Status == status);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(u => u.AppliedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(u => u.AppliedAt <= endDate.Value);
                }

                var totalCount = await query.CountAsync();

                var updates = await query
                    .OrderByDescending(u => u.AppliedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Version,
                        AppliedAt = u.AppliedAt,
                        u.Status,
                        u.ReleaseNotes,
                        u.ErrorMessage,
                        u.ApplyDurationSeconds
                    })
                    .ToListAsync();

                // Convert UTC timestamps to Central Time
                var updatesConverted = updates.Select(u => new
                {
                    u.Id,
                    u.Version,
                    AppliedAt = _timezoneService.ConvertUtcToLocalTime(u.AppliedAt),
                    u.Status,
                    u.ReleaseNotes,
                    u.ErrorMessage,
                    u.ApplyDurationSeconds
                }).ToList();

                // Get statistics
                var stats = new
                {
                    totalUpdates = totalCount,
                    successful = await _context.UpdateHistories.CountAsync(u => u.Status == "Success"),
                    failed = await _context.UpdateHistories.CountAsync(u => u.Status == "Failed"),
                    rolledBack = await _context.UpdateHistories.CountAsync(u => u.Status == "RolledBack")
                };

                return Ok(new
                {
                    success = true,
                    data = updatesConverted,
                    totalCount,
                    page,
                    pageSize,
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving update history");
                return StatusCode(500, new { success = false, message = "Error retrieving update history" });
            }
        }

        // GET: api/admin/service/metrics
        [HttpGet("metrics")]
        public async Task<IActionResult> GetPerformanceMetrics([FromQuery] int hours = 24)
        {
            try
            {
                if (hours < 1 || hours > 168) // Max 7 days
                {
                    return BadRequest(new { success = false, message = "Hours must be between 1 and 168" });
                }

                var startTime = DateTime.UtcNow.AddHours(-hours);

                // Get status logs for the time period
                var statusLogs = await _context.ServiceStatusLogs
                    .Where(s => s.Timestamp >= startTime)
                    .OrderBy(s => s.Timestamp)
                    .ToListAsync();

                if (!statusLogs.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            message = "No metrics available for the specified time period",
                            hours
                        }
                    });
                }

                // Calculate metrics (handle empty collection)
                var avgResponseTime = statusLogs.Any() ? statusLogs.Average(s => s.ResponseTimeMs) : 0;
                var avgMemoryUsage = statusLogs.Any() ? statusLogs.Average(s => s.MemoryUsageMb) : 0;
                var avgCpuUsage = statusLogs.Any() ? statusLogs.Average(s => (double)s.CpuUsagePercent) : 0;
                var maxResponseTime = statusLogs.Any() ? statusLogs.Max(s => s.ResponseTimeMs) : 0;
                var maxMemoryUsage = statusLogs.Any() ? statusLogs.Max(s => s.MemoryUsageMb) : 0;
                var maxCpuUsage = statusLogs.Any() ? statusLogs.Max(s => s.CpuUsagePercent) : 0;

                // Calculate uptime
                var totalChecks = statusLogs.Count;
                var onlineChecks = statusLogs.Count(s => s.IsWebServiceOnline);
                var uptimePercent = totalChecks > 0 ? (decimal)onlineChecks / totalChecks * 100 : 100;

                // Get error counts
                var errorStartTime = DateTime.UtcNow.AddHours(-24);
                var errors24h = await _context.ServiceErrorLogs
                    .Where(e => e.Timestamp >= errorStartTime)
                    .ToListAsync();

                var totalErrors = errors24h.Count;
                var criticalErrors = errors24h.Count(e => e.Severity == "Critical");
                var errorsBySource = errors24h
                    .GroupBy(e => e.Source)
                    .Select(g => new { Source = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // Hourly breakdown
                var hourlyMetrics = statusLogs
                    .GroupBy(s => new { s.Timestamp.Year, s.Timestamp.Month, s.Timestamp.Day, s.Timestamp.Hour })
                    .Select(g => new
                    {
                        Timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                        AvgResponseTime = Math.Round(g.Average(s => s.ResponseTimeMs), 2),
                        AvgMemoryUsage = Math.Round(g.Average(s => s.MemoryUsageMb), 2),
                        AvgCpuUsage = Math.Round(g.Average(s => (double)s.CpuUsagePercent), 2),
                        OnlineCount = g.Count(s => s.IsWebServiceOnline),
                        TotalChecks = g.Count()
                    })
                    .OrderBy(h => h.Timestamp)
                    .ToList();

                // Convert hourly metrics timestamps to Central Time
                var hourlyMetricsConverted = hourlyMetrics.Select(h => new
                {
                    Timestamp = _timezoneService.ConvertUtcToLocalTime(h.Timestamp),
                    h.AvgResponseTime,
                    h.AvgMemoryUsage,
                    h.AvgCpuUsage,
                    h.OnlineCount,
                    h.TotalChecks
                }).ToList();

                var centralStartTime = _timezoneService.ConvertUtcToLocalTime(startTime);
                var centralEndTime = _timezoneService.ConvertUtcToLocalTime(DateTime.UtcNow);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        timeRange = new { startTime = centralStartTime, endTime = centralEndTime, hours },
                        summary = new
                        {
                            avgResponseTimeMs = Math.Round(avgResponseTime, 2),
                            maxResponseTimeMs = maxResponseTime,
                            avgMemoryUsageMb = Math.Round(avgMemoryUsage, 2),
                            maxMemoryUsageMb = maxMemoryUsage,
                            avgCpuUsagePercent = Math.Round(avgCpuUsage, 2),
                            maxCpuUsagePercent = Math.Round(maxCpuUsage, 2),
                            uptimePercent = Math.Round(uptimePercent, 2),
                            totalErrors24h = totalErrors,
                            criticalErrors24h = criticalErrors
                        },
                        errorsBySource,
                        hourlyMetrics = hourlyMetricsConverted
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics");
                return StatusCode(500, new { success = false, message = "Error retrieving performance metrics" });
            }
        }

        // GET: api/admin/service/health
        [HttpGet("health")]
        [AllowAnonymous] // Health check endpoint can be accessed without authentication for monitoring tools
        public async Task<IActionResult> GetHealthCheck()
        {
            try
            {
                // Quick health check
                var dbConnected = await _context.Database.CanConnectAsync();

                var latestStatus = await _context.ServiceStatusLogs
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                var isHealthy = dbConnected &&
                               (latestStatus == null || (latestStatus.IsWebServiceOnline && latestStatus.DatabaseConnected));

                return Ok(new
                {
                    status = isHealthy ? "healthy" : "unhealthy",
                    timestamp = _timezoneService.ConvertUtcToLocalTime(DateTime.UtcNow),
                    checks = new
                    {
                        database = dbConnected ? "up" : "down",
                        webService = latestStatus?.IsWebServiceOnline ?? true ? "up" : "down",
                        lastCheck = latestStatus != null ? _timezoneService.ConvertUtcToLocalTime(latestStatus.Timestamp) : (DateTime?)null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    timestamp = _timezoneService.ConvertUtcToLocalTime(DateTime.UtcNow),
                    error = "Health check failed"
                });
            }
        }

        // POST: api/admin/service/errors/log
        [HttpPost("errors/log")]
        [AllowAnonymous] // Allow Windows Service to log without authentication
        public async Task<IActionResult> LogServiceError([FromBody] ServiceErrorLogDto errorLog)
        {
            try
            {
                if (errorLog == null)
                {
                    return BadRequest(new { success = false, message = "Error log data is required" });
                }

                var newErrorLog = new ServiceErrorLog
                {
                    Timestamp = DateTime.UtcNow,
                    Source = errorLog.Source,
                    Severity = errorLog.Severity,
                    Message = errorLog.Message,
                    StackTrace = errorLog.StackTrace,
                    IsAcknowledged = false
                };

                _context.ServiceErrorLogs.Add(newErrorLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Service error logged: {Source} - {Severity}", errorLog.Source, errorLog.Severity);

                return Ok(new { success = true, message = "Error logged successfully", id = newErrorLog.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging service error");
                return StatusCode(500, new { success = false, message = "Error logging service error" });
            }
        }

        // POST: api/admin/service/updates/log
        [HttpPost("updates/log")]
        [AllowAnonymous] // Allow Windows Service to log without authentication
        public async Task<IActionResult> LogUpdateHistory([FromBody] UpdateHistoryLogDto updateLog)
        {
            try
            {
                if (updateLog == null)
                {
                    return BadRequest(new { success = false, message = "Update log data is required" });
                }

                var newUpdateLog = new UpdateHistory
                {
                    Version = updateLog.Version,
                    AppliedAt = updateLog.AppliedAt,
                    Status = updateLog.Status,
                    ReleaseNotes = updateLog.ReleaseNotes,
                    ErrorMessage = updateLog.ErrorMessage,
                    ApplyDurationSeconds = updateLog.ApplyDurationSeconds
                };

                _context.UpdateHistories.Add(newUpdateLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Update history logged: {Version} - {Status}", updateLog.Version, updateLog.Status);

                return Ok(new { success = true, message = "Update logged successfully", id = newUpdateLog.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging update history");
                return StatusCode(500, new { success = false, message = "Error logging update history" });
            }
        }
    }

    // Request DTOs
    public class AcknowledgeAllRequest
    {
        public string? Severity { get; set; }
        public string? Source { get; set; }
    }
}
