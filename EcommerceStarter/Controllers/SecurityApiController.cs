using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/security")]
    public class SecurityApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SecurityApiController> _logger;

        public SecurityApiController(ApplicationDbContext context, ILogger<SecurityApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/security/blocked-ips
        [HttpGet("blocked-ips")]
        public async Task<IActionResult> GetBlockedIps(
            [FromQuery] bool includeExpired = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.BlockedIps.AsQueryable();

                // Filter out expired blocks unless explicitly requested
                if (!includeExpired)
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(b => b.IsPermanent || b.ExpiresAt == null || b.ExpiresAt > now);
                }

                var totalCount = await query.CountAsync();

                var blockedIps = await query
                    .OrderByDescending(b => b.BlockedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.Id,
                        b.IpAddress,
                        b.BlockedAt,
                        b.ExpiresAt,
                        b.Reason,
                        b.OffenseCount,
                        b.IsPermanent,
                        IsActive = b.IsPermanent || b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow,
                        TimeRemaining = b.ExpiresAt.HasValue && !b.IsPermanent
                            ? (b.ExpiresAt.Value - DateTime.UtcNow).TotalMinutes
                            : (double?)null
                    })
                    .ToListAsync();

                // Get statistics
                var stats = new
                {
                    totalBlocked = totalCount,
                    activeBlocks = blockedIps.Count(b => b.IsActive),
                    permanentBlocks = await _context.BlockedIps.CountAsync(b => b.IsPermanent),
                    expiredBlocks = await _context.BlockedIps.CountAsync(b => !b.IsPermanent && b.ExpiresAt < DateTime.UtcNow)
                };

                return Ok(new
                {
                    success = true,
                    data = blockedIps,
                    totalCount,
                    page,
                    pageSize,
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blocked IPs");
                return StatusCode(500, new { success = false, message = "Error retrieving blocked IPs" });
            }
        }

        // GET: api/security/blocked-ips/{ip}
        [HttpGet("blocked-ips/{ip}")]
        public async Task<IActionResult> GetBlockedIp(string ip)
        {
            try
            {
                var blockedIp = await _context.BlockedIps
                    .Where(b => b.IpAddress == ip)
                    .Select(b => new
                    {
                        b.Id,
                        b.IpAddress,
                        b.BlockedAt,
                        b.ExpiresAt,
                        b.Reason,
                        b.OffenseCount,
                        b.IsPermanent,
                        IsActive = b.IsPermanent || b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow
                    })
                    .FirstOrDefaultAsync();

                if (blockedIp == null)
                {
                    return NotFound(new { success = false, message = "Blocked IP not found" });
                }

                // Get related security events for this IP
                var recentEvents = await _context.SecurityAuditLogs
                    .Where(log => log.IpAddress == ip)
                    .OrderByDescending(log => log.Timestamp)
                    .Take(10)
                    .Select(log => new
                    {
                        log.Id,
                        log.Timestamp,
                        log.EventType,
                        log.Severity,
                        log.UserEmail,
                        log.Details
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        blockInfo = blockedIp,
                        recentEvents
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving blocked IP {IpAddress}", ip);
                return StatusCode(500, new { success = false, message = "Error retrieving blocked IP" });
            }
        }

        // DELETE: api/security/blocked-ips/{ip}
        [HttpDelete("blocked-ips/{ip}")]
        public async Task<IActionResult> UnblockIp(string ip)
        {
            try
            {
                var blockedIp = await _context.BlockedIps
                    .FirstOrDefaultAsync(b => b.IpAddress == ip);

                if (blockedIp == null)
                {
                    return NotFound(new { success = false, message = "Blocked IP not found" });
                }

                _context.BlockedIps.Remove(blockedIp);
                await _context.SaveChangesAsync();

                // Log the unblock action
                _context.SecurityAuditLogs.Add(new SecurityAuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "IP Unblocked",
                    Severity = "Low",
                    IpAddress = ip,
                    Details = $"IP {ip} was unblocked by admin. Previous reason: {blockedIp.Reason}",
                    UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                    Endpoint = "/api/security/blocked-ips",
                    IsBlocked = false
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("IP {IpAddress} unblocked by admin", ip);

                return Ok(new
                {
                    success = true,
                    message = "IP unblocked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking IP {IpAddress}", ip);
                return StatusCode(500, new { success = false, message = "Error unblocking IP" });
            }
        }

        // POST: api/security/blocked-ips
        [HttpPost("blocked-ips")]
        public async Task<IActionResult> BlockIp([FromBody] BlockIpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IpAddress))
                {
                    return BadRequest(new { success = false, message = "IP address is required" });
                }

                // Check if IP is already blocked
                var existingBlock = await _context.BlockedIps
                    .FirstOrDefaultAsync(b => b.IpAddress == request.IpAddress);

                if (existingBlock != null)
                {
                    return BadRequest(new { success = false, message = "IP address is already blocked" });
                }

                var blockedIp = new BlockedIp
                {
                    IpAddress = request.IpAddress.Trim(),
                    BlockedAt = DateTime.UtcNow,
                    Reason = request.Reason ?? "Manually blocked by admin",
                    IsPermanent = request.IsPermanent ?? false,
                    ExpiresAt = request.IsPermanent == true ? null : DateTime.UtcNow.AddMinutes(request.DurationMinutes ?? 30),
                    OffenseCount = 1
                };

                _context.BlockedIps.Add(blockedIp);

                // Log the block action
                _context.SecurityAuditLogs.Add(new SecurityAuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    EventType = "IP Blocked",
                    Severity = "Medium",
                    IpAddress = request.IpAddress,
                    Details = $"IP manually blocked by admin. Reason: {blockedIp.Reason}. Permanent: {blockedIp.IsPermanent}",
                    UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                    Endpoint = "/api/security/blocked-ips",
                    IsBlocked = true
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("IP {IpAddress} blocked by admin. Permanent: {IsPermanent}", request.IpAddress, blockedIp.IsPermanent);

                return CreatedAtAction(nameof(GetBlockedIp), new { ip = blockedIp.IpAddress }, new
                {
                    success = true,
                    message = "IP blocked successfully",
                    data = new
                    {
                        blockedIp.Id,
                        blockedIp.IpAddress,
                        blockedIp.BlockedAt,
                        blockedIp.ExpiresAt,
                        blockedIp.Reason,
                        blockedIp.IsPermanent
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking IP");
                return StatusCode(500, new { success = false, message = "Error blocking IP" });
            }
        }

        // GET: api/security/audit-log
        [HttpGet("audit-log")]
        public async Task<IActionResult> GetSecurityAuditLog(
            [FromQuery] string? eventType = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? ipAddress = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.SecurityAuditLogs.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(eventType))
                {
                    query = query.Where(log => log.EventType == eventType);
                }

                if (!string.IsNullOrWhiteSpace(severity))
                {
                    query = query.Where(log => log.Severity == severity);
                }

                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    query = query.Where(log => log.IpAddress == ipAddress);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp <= endDate.Value);
                }

                var totalCount = await query.CountAsync();

                var auditLogs = await query
                    .OrderByDescending(log => log.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(log => new
                    {
                        log.Id,
                        log.Timestamp,
                        log.EventType,
                        log.Severity,
                        log.IpAddress,
                        log.UserEmail,
                        log.UserId,
                        log.Details,
                        log.Endpoint,
                        log.UserAgent,
                        log.IsBlocked
                    })
                    .ToListAsync();

                // Get available filter options
                var eventTypes = await _context.SecurityAuditLogs
                    .Select(log => log.EventType)
                    .Distinct()
                    .OrderBy(et => et)
                    .ToListAsync();

                var severities = await _context.SecurityAuditLogs
                    .Where(log => log.Severity != null)
                    .Select(log => log.Severity!)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync();

                // Get statistics
                var stats = new
                {
                    totalEvents = totalCount,
                    criticalEvents = await _context.SecurityAuditLogs.CountAsync(log => log.Severity == "Critical"),
                    blockedEvents = await _context.SecurityAuditLogs.CountAsync(log => log.IsBlocked),
                    last24Hours = await _context.SecurityAuditLogs
                        .CountAsync(log => log.Timestamp >= DateTime.UtcNow.AddHours(-24))
                };

                return Ok(new
                {
                    success = true,
                    data = auditLogs,
                    totalCount,
                    page,
                    pageSize,
                    filters = new
                    {
                        availableEventTypes = eventTypes,
                        availableSeverities = severities
                    },
                    statistics = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security audit log");
                return StatusCode(500, new { success = false, message = "Error retrieving security audit log" });
            }
        }

        // GET: api/security/audit-log/{id}
        [HttpGet("audit-log/{id}")]
        public async Task<IActionResult> GetSecurityAuditLogEntry(int id)
        {
            try
            {
                var auditLog = await _context.SecurityAuditLogs
                    .Where(log => log.Id == id)
                    .Select(log => new
                    {
                        log.Id,
                        log.Timestamp,
                        log.EventType,
                        log.Severity,
                        log.IpAddress,
                        log.UserEmail,
                        log.UserId,
                        log.Details,
                        log.Endpoint,
                        log.UserAgent,
                        log.IsBlocked
                    })
                    .FirstOrDefaultAsync();

                if (auditLog == null)
                {
                    return NotFound(new { success = false, message = "Audit log entry not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = auditLog
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log entry {AuditLogId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving audit log entry" });
            }
        }

        // GET: api/security/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetSecurityDashboard()
        {
            try
            {
                var now = DateTime.UtcNow;
                var last24Hours = now.AddHours(-24);
                var last7Days = now.AddDays(-7);

                // Get active blocks
                var activeBlocks = await _context.BlockedIps
                    .Where(b => b.IsPermanent || b.ExpiresAt == null || b.ExpiresAt > now)
                    .CountAsync();

                // Get recent security events (last 24 hours)
                var recentEvents = await _context.SecurityAuditLogs
                    .Where(log => log.Timestamp >= last24Hours)
                    .GroupBy(log => log.Severity)
                    .Select(g => new
                    {
                        Severity = g.Key ?? "Unknown",
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get top blocked IPs by offense count
                var topOffenders = await _context.BlockedIps
                    .OrderByDescending(b => b.OffenseCount)
                    .Take(5)
                    .Select(b => new
                    {
                        b.IpAddress,
                        b.OffenseCount,
                        b.Reason,
                        b.BlockedAt
                    })
                    .ToListAsync();

                // Get event type breakdown (last 7 days)
                var eventTypeBreakdown = await _context.SecurityAuditLogs
                    .Where(log => log.Timestamp >= last7Days)
                    .GroupBy(log => log.EventType)
                    .Select(g => new
                    {
                        EventType = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                // Get trend data (daily breakdown for last 7 days)
                var dailyTrends = await _context.SecurityAuditLogs
                    .Where(log => log.Timestamp >= last7Days)
                    .GroupBy(log => log.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TotalEvents = g.Count(),
                        CriticalEvents = g.Count(log => log.Severity == "Critical"),
                        BlockedEvents = g.Count(log => log.IsBlocked)
                    })
                    .OrderBy(d => d.Date)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        summary = new
                        {
                            activeBlocks,
                            totalEvents24h = recentEvents.Sum(e => e.Count),
                            criticalEvents24h = recentEvents.FirstOrDefault(e => e.Severity == "Critical")?.Count ?? 0,
                            last24HoursBySeverity = recentEvents
                        },
                        topOffenders,
                        eventTypeBreakdown,
                        dailyTrends,
                        timestamp = now
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving security dashboard");
                return StatusCode(500, new { success = false, message = "Error retrieving security dashboard" });
            }
        }
    }

    // Request DTOs
    public class BlockIpRequest
    {
        public string IpAddress { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool? IsPermanent { get; set; }
        public int? DurationMinutes { get; set; }
    }
}
