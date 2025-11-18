using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    public interface ISecurityAuditService
    {
        Task LogSecurityEventAsync(string eventType, string severity, string ipAddress, 
            string? userId = null, string? userEmail = null, string? details = null, 
            string? endpoint = null, string? userAgent = null, bool isBlocked = false);
        
        Task<bool> IsIpBlockedAsync(string ipAddress);
        Task BlockIpAsync(string ipAddress, string reason, int durationMinutes = 60, bool isPermanent = false);
        Task<int> GetFailedLoginAttemptsAsync(string ipAddress, int withinMinutes = 15);
        Task<List<SecurityAuditLog>> GetRecentSecurityEventsAsync(int count = 100);
        Task<List<BlockedIp>> GetBlockedIpsAsync();
        Task UnblockIpAsync(string ipAddress);
    }

    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SecurityAuditService> _logger;
        private readonly IQueuedEventService? _queuedEventService;

        public SecurityAuditService(
            ApplicationDbContext context, 
            ILogger<SecurityAuditService> logger,
            IQueuedEventService? queuedEventService = null)
        {
            _context = context;
            _logger = logger;
            _queuedEventService = queuedEventService;
        }

        public async Task LogSecurityEventAsync(string eventType, string severity, string ipAddress, 
            string? userId = null, string? userEmail = null, string? details = null, 
            string? endpoint = null, string? userAgent = null, bool isBlocked = false)
        {
            try
            {
                // For critical events, write immediately to database
                if (severity == "Critical" || severity == "High" || isBlocked)
                {
                    var logEntry = new SecurityAuditLog
                    {
                        EventType = eventType,
                        Severity = severity,
                        IpAddress = ipAddress,
                        UserId = userId,
                        UserEmail = userEmail,
                        Details = details,
                        Endpoint = endpoint,
                        UserAgent = userAgent,
                        IsBlocked = isBlocked,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.SecurityAuditLogs.Add(logEntry);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Security Event: {EventType} | Severity: {Severity} | IP: {IpAddress} | User: {UserEmail} | Details: {Details}",
                        eventType, severity, ipAddress, userEmail ?? "Unknown", details ?? "None");
                }
                else if (_queuedEventService != null)
                {
                    // For non-critical events, queue for batch processing
                    _queuedEventService.QueueSecurityAudit(eventType, severity, ipAddress, 
                        userId, userEmail, details, endpoint, userAgent, isBlocked);
                }
                else
                {
                    // Fallback: write directly if queue service not available
                    var logEntry = new SecurityAuditLog
                    {
                        EventType = eventType,
                        Severity = severity,
                        IpAddress = ipAddress,
                        UserId = userId,
                        UserEmail = userEmail,
                        Details = details,
                        Endpoint = endpoint,
                        UserAgent = userAgent,
                        IsBlocked = isBlocked,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.SecurityAuditLogs.Add(logEntry);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType} for IP: {IpAddress}", eventType, ipAddress);
            }
        }

        public async Task<bool> IsIpBlockedAsync(string ipAddress)
        {
            var blockedIp = await _context.BlockedIps
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress);

            if (blockedIp == null)
                return false;

            // Check if block has expired
            if (!blockedIp.IsPermanent && blockedIp.ExpiresAt.HasValue && blockedIp.ExpiresAt < DateTime.UtcNow)
            {
                _context.BlockedIps.Remove(blockedIp);
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }

        public async Task BlockIpAsync(string ipAddress, string reason, int durationMinutes = 60, bool isPermanent = false)
        {
            var existingBlock = await _context.BlockedIps
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress);

            if (existingBlock != null)
            {
                // Update existing block
                existingBlock.OffenseCount++;
                existingBlock.BlockedAt = DateTime.UtcNow;
                existingBlock.ExpiresAt = isPermanent ? null : DateTime.UtcNow.AddMinutes(durationMinutes);
                existingBlock.IsPermanent = isPermanent;
                existingBlock.Reason = $"{existingBlock.Reason}; {reason}";
            }
            else
            {
                // Create new block
                var blockedIp = new BlockedIp
                {
                    IpAddress = ipAddress,
                    BlockedAt = DateTime.UtcNow,
                    ExpiresAt = isPermanent ? null : DateTime.UtcNow.AddMinutes(durationMinutes),
                    Reason = reason,
                    IsPermanent = isPermanent,
                    OffenseCount = 1
                };
                _context.BlockedIps.Add(blockedIp);
            }

            await _context.SaveChangesAsync();

            await LogSecurityEventAsync("IpBlocked", "High", ipAddress, 
                details: $"IP blocked for {durationMinutes} minutes. Reason: {reason}", 
                isBlocked: true);
        }

        public async Task<int> GetFailedLoginAttemptsAsync(string ipAddress, int withinMinutes = 15)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-withinMinutes);
            
            return await _context.SecurityAuditLogs
                .Where(log => log.IpAddress == ipAddress 
                    && log.EventType == "FailedLogin" 
                    && log.Timestamp >= cutoffTime)
                .CountAsync();
        }

        public async Task<List<SecurityAuditLog>> GetRecentSecurityEventsAsync(int count = 100)
        {
            return await _context.SecurityAuditLogs
                .OrderByDescending(log => log.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<BlockedIp>> GetBlockedIpsAsync()
        {
            return await _context.BlockedIps
                .OrderByDescending(b => b.BlockedAt)
                .ToListAsync();
        }

        public async Task UnblockIpAsync(string ipAddress)
        {
            var blockedIp = await _context.BlockedIps
                .FirstOrDefaultAsync(b => b.IpAddress == ipAddress);

            if (blockedIp != null)
            {
                _context.BlockedIps.Remove(blockedIp);
                await _context.SaveChangesAsync();

                await LogSecurityEventAsync("IpUnblocked", "Medium", ipAddress, 
                    details: "IP manually unblocked by administrator");
            }
        }
    }
}
