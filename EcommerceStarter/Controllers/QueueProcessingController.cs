using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceStarter.Controllers
{
    /// <summary>
    /// API endpoint for background queue processing (called by Windows Service)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QueueProcessingController : ControllerBase
    {
        private readonly IQueuedEventService _queuedEventService;
        private readonly ILogger<QueueProcessingController> _logger;

        public QueueProcessingController(
            IQueuedEventService queuedEventService,
            ILogger<QueueProcessingController> logger)
        {
            _queuedEventService = queuedEventService;
            _logger = logger;
        }

        /// <summary>
        /// Process all queued events (called by Windows Service)
        /// No authentication required - internal service endpoint
        /// </summary>
        [HttpPost("process")]
        [AllowAnonymous] // Internal service endpoint
        public async Task<IActionResult> ProcessQueue(CancellationToken cancellationToken)
        {
            try
            {
                // Only allow local requests for security
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (remoteIp != "::1" && remoteIp != "127.0.0.1" && !remoteIp?.StartsWith("::ffff:127.0.0.1") == true)
                {
                    _logger.LogWarning("Queue processing attempted from non-local IP: {IP}", remoteIp);
                    return Forbid();
                }

                var processed = await _queuedEventService.ProcessQueuedEventsAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    eventsProcessed = processed,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued events via API");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Internal server error processing queue"
                });
            }
        }

        /// <summary>
        /// Get queue statistics
        /// </summary>
        [HttpGet("stats")]
        [AllowAnonymous] // Internal service endpoint
        public IActionResult GetQueueStats()
        {
            try
            {
                // Only allow local requests
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (remoteIp != "::1" && remoteIp != "127.0.0.1" && !remoteIp?.StartsWith("::ffff:127.0.0.1") == true)
                {
                    return Forbid();
                }

                var queueSize = _queuedEventService.GetQueueSize();

                return Ok(new
                {
                    queueSize = queueSize,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get detailed queue metrics (for monitoring)
        /// </summary>
        [HttpGet("metrics")]
        [AllowAnonymous] // Internal service endpoint
        public IActionResult GetQueueMetrics()
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (remoteIp != "::1" && remoteIp != "127.0.0.1" && !remoteIp?.StartsWith("::ffff:127.0.0.1") == true)
                {
                    return Forbid();
                }

                var metrics = ((QueuedEventService)_queuedEventService).GetMetrics();

                return Ok(new
                {
                    totalEventsProcessed = metrics.TotalEventsProcessed,
                    totalFailures = metrics.TotalFailures,
                    peakQueueSize = metrics.PeakQueueSize,
                    currentQueueSize = metrics.CurrentQueueSize,
                    pageViewQueueSize = metrics.PageViewQueueSize,
                    visitorEventQueueSize = metrics.VisitorEventQueueSize,
                    securityAuditQueueSize = metrics.SecurityAuditQueueSize,
                    customerAuditQueueSize = metrics.CustomerAuditQueueSize,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue metrics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
