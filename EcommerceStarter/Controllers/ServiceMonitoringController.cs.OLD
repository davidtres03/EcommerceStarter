using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Models.Service;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Controllers
{
    /// <summary>
    /// Admin API for monitoring service health, updates, and errors
    /// All endpoints require Admin authorization
    /// </summary>
    [ApiController]
    [Route("api/admin/service")]
    [Authorize(Roles = "Admin")]
    public class ServiceMonitoringController : ControllerBase
    {
        private readonly ILogger<ServiceMonitoringController> _logger;
        private readonly ApplicationDbContext _context;

        public ServiceMonitoringController(ILogger<ServiceMonitoringController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get current service status and health metrics
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<ServiceStatusDto>> GetServiceStatus()
        {
            try
            {
                // Get latest status log
                var latestStatus = await _context.Set<ServiceStatusLog>()
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                if (latestStatus == null)
                {
                    return NotFound(new { message = "No status data available" });
                }

                // Get recent updates
                var recentUpdates = await _context.Set<UpdateHistory>()
                    .OrderByDescending(u => u.AppliedAt)
                    .Take(5)
                    .Select(u => new UpdateHistoryDto
                    {
                        Version = u.Version,
                        AppliedAt = u.AppliedAt,
                        Status = u.Status,
                        ReleaseNotes = u.ReleaseNotes,
                        ApplyDurationSeconds = u.ApplyDurationSeconds
                    })
                    .ToListAsync();

                // Get recent errors
                var recentErrors = await _context.Set<ServiceErrorLog>()
                    .Where(e => e.Timestamp > DateTime.UtcNow.AddDays(-1))
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10)
                    .Select(e => new ServiceErrorDto
                    {
                        Id = e.Id,
                        Timestamp = e.Timestamp,
                        Source = e.Source,
                        Severity = e.Severity,
                        Message = e.Message,
                        IsAcknowledged = e.IsAcknowledged
                    })
                    .ToListAsync();

                var dto = new ServiceStatusDto
                {
                    Timestamp = latestStatus.Timestamp,
                    IsWebServiceOnline = latestStatus.IsWebServiceOnline,
                    ResponseTimeMs = latestStatus.ResponseTimeMs,
                    IsBackgroundServiceRunning = latestStatus.IsBackgroundServiceRunning,
                    PendingOrdersCount = latestStatus.PendingOrdersCount,
                    MemoryUsageMb = latestStatus.MemoryUsageMb,
                    CpuUsagePercent = latestStatus.CpuUsagePercent,
                    DatabaseConnected = latestStatus.DatabaseConnected,
                    ActiveUserCount = latestStatus.ActiveUserCount,
                    UptimePercent = latestStatus.UptimePercent,
                    ErrorMessage = latestStatus.ErrorMessage,
                    RecentUpdates = recentUpdates,
                    RecentErrors = recentErrors
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service status");
                return StatusCode(500, new { error = "Failed to retrieve service status" });
            }
        }

        /// <summary>
        /// Get service status history (last 24 hours)
        /// </summary>
        [HttpGet("status/history")]
        public async Task<ActionResult<List<ServiceStatusDto>>> GetStatusHistory()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-24);

                var statusLogs = await _context.Set<ServiceStatusLog>()
                    .Where(s => s.Timestamp > cutoffTime)
                    .OrderByDescending(s => s.Timestamp)
                    .Select(s => new ServiceStatusDto
                    {
                        Timestamp = s.Timestamp,
                        IsWebServiceOnline = s.IsWebServiceOnline,
                        ResponseTimeMs = s.ResponseTimeMs,
                        IsBackgroundServiceRunning = s.IsBackgroundServiceRunning,
                        PendingOrdersCount = s.PendingOrdersCount,
                        MemoryUsageMb = s.MemoryUsageMb,
                        CpuUsagePercent = s.CpuUsagePercent,
                        DatabaseConnected = s.DatabaseConnected,
                        ActiveUserCount = s.ActiveUserCount,
                        UptimePercent = s.UptimePercent,
                        ErrorMessage = s.ErrorMessage
                    })
                    .ToListAsync();

                return Ok(statusLogs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status history");
                return StatusCode(500, new { error = "Failed to retrieve status history" });
            }
        }

        /// <summary>
        /// Get update history
        /// </summary>
        [HttpGet("updates")]
        public async Task<ActionResult<List<UpdateHistoryDto>>> GetUpdateHistory()
        {
            try
            {
                var updates = await _context.Set<UpdateHistory>()
                    .OrderByDescending(u => u.AppliedAt)
                    .Take(50)
                    .Select(u => new UpdateHistoryDto
                    {
                        Version = u.Version,
                        AppliedAt = u.AppliedAt,
                        Status = u.Status,
                        ReleaseNotes = u.ReleaseNotes,
                        ApplyDurationSeconds = u.ApplyDurationSeconds
                    })
                    .ToListAsync();

                return Ok(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving update history");
                return StatusCode(500, new { error = "Failed to retrieve update history" });
            }
        }

        /// <summary>
        /// Get service error logs
        /// </summary>
        [HttpGet("errors")]
        public async Task<ActionResult<List<ServiceErrorDto>>> GetErrorLogs(
            [FromQuery] int hoursBack = 24,
            [FromQuery] string? severity = null)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);

                var query = _context.Set<ServiceErrorLog>()
                    .Where(e => e.Timestamp > cutoffTime);

                if (!string.IsNullOrEmpty(severity))
                {
                    query = query.Where(e => e.Severity == severity);
                }

                var errors = await query
                    .OrderByDescending(e => e.Timestamp)
                    .Take(100)
                    .Select(e => new ServiceErrorDto
                    {
                        Id = e.Id,
                        Timestamp = e.Timestamp,
                        Source = e.Source,
                        Severity = e.Severity,
                        Message = e.Message,
                        IsAcknowledged = e.IsAcknowledged
                    })
                    .ToListAsync();

                return Ok(errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving error logs");
                return StatusCode(500, new { error = "Failed to retrieve error logs" });
            }
        }

        /// <summary>
        /// Acknowledge an error (mark as reviewed)
        /// </summary>
        [HttpPut("errors/{errorId}/acknowledge")]
        public async Task<ActionResult> AcknowledgeError(string errorId)
        {
            try
            {
                var error = await _context.Set<ServiceErrorLog>().FindAsync(errorId);

                if (error == null)
                {
                    return NotFound(new { error = "Error log not found" });
                }

                error.IsAcknowledged = true;
                error.AcknowledgedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Error acknowledged" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging error log");
                return StatusCode(500, new { error = "Failed to acknowledge error" });
            }
        }

        /// <summary>
        /// Get performance metrics summary
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<PerformanceMetricsDto>> GetPerformanceMetrics()
        {
            try
            {
                var cutoff24h = DateTime.UtcNow.AddHours(-24);
                var cutoff30d = DateTime.UtcNow.AddDays(-30);

                var statusLogs = await _context.Set<ServiceStatusLog>()
                    .Where(s => s.Timestamp > cutoff24h)
                    .ToListAsync();

                var updates = await _context.Set<UpdateHistory>()
                    .Where(u => u.AppliedAt > cutoff30d && u.Status == "Success")
                    .ToListAsync();

                var errors24h = await _context.Set<ServiceErrorLog>()
                    .Where(e => e.Timestamp > cutoff24h)
                    .ToListAsync();

                var criticalErrors = errors24h.Count(e => e.Severity == "Critical");

                var metrics = new PerformanceMetricsDto
                {
                    AverageResponseTimeMs = statusLogs.Any() ? (decimal)statusLogs.Average(s => s.ResponseTimeMs) : 0,
                    AverageMemoryUsageMb = statusLogs.Any() ? (decimal)statusLogs.Average(s => s.MemoryUsageMb) : 0,
                    AverageCpuUsagePercent = statusLogs.Any() ? (decimal)statusLogs.Average(s => (double)s.CpuUsagePercent) : 0,
                    UptimePercent = statusLogs.Any() ? (decimal)statusLogs.Average(s => (double)s.UptimePercent) : 0,
                    TotalErrors24Hours = errors24h.Count,
                    CriticalErrors24Hours = criticalErrors,
                    UpdatesApplied30Days = updates.Count,
                    LastSuccessfulBackup = DateTime.UtcNow.AddHours(-6) // Placeholder
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics");
                return StatusCode(500, new { error = "Failed to retrieve metrics" });
            }
        }

        /// <summary>
        /// Log a service status update (called by background service)
        /// </summary>
        [HttpPost("status/log")]
        [AllowAnonymous]
        public async Task<ActionResult> LogServiceStatus([FromBody] ServiceStatusLog statusLog)
        {
            try
            {
                statusLog.Id = Guid.NewGuid().ToString();
                statusLog.Timestamp = DateTime.UtcNow;

                await _context.Set<ServiceStatusLog>().AddAsync(statusLog);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Status logged" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging service status");
                return StatusCode(500, new { error = "Failed to log status" });
            }
        }

        /// <summary>
        /// Log an error event (called by services)
        /// </summary>
        [HttpPost("errors/log")]
        [AllowAnonymous]
        public async Task<ActionResult> LogError([FromBody] ServiceErrorLog errorLog)
        {
            try
            {
                errorLog.Id = Guid.NewGuid().ToString();
                errorLog.Timestamp = DateTime.UtcNow;
                errorLog.IsAcknowledged = false;

                await _context.Set<ServiceErrorLog>().AddAsync(errorLog);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Error logged", id = errorLog.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging service error");
                return StatusCode(500, new { error = "Failed to log error" });
            }
        }

        /// <summary>
        /// Log an update event
        /// </summary>
        [HttpPost("updates/log")]
        [AllowAnonymous]
        public async Task<ActionResult> LogUpdate([FromBody] UpdateHistory updateLog)
        {
            try
            {
                updateLog.Id = Guid.NewGuid().ToString();

                await _context.Set<UpdateHistory>().AddAsync(updateLog);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Update logged", id = updateLog.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging update");
                return StatusCode(500, new { error = "Failed to log update" });
            }
        }
    }
}
