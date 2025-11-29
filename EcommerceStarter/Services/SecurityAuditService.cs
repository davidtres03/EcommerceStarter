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
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IEmailService? _emailService;

        public SecurityAuditService(
            ApplicationDbContext context, 
            ILogger<SecurityAuditService> logger,
            ISecuritySettingsService securitySettingsService,
            IEmailService? emailService = null,
            IQueuedEventService? queuedEventService = null)
        {
            _context = context;
            _logger = logger;
            _queuedEventService = queuedEventService;
            _securitySettingsService = securitySettingsService;
            _emailService = emailService;
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

                    // Auto-block on failed login burst
                    if (eventType == "FailedLogin")
                    {
                        var settings = await _securitySettingsService.GetSettingsAsync();
                        if (settings.EnableIpBlocking)
                        {
                            var withinMinutes = settings.FailedLoginBurstWindowMinutes > 0 ? settings.FailedLoginBurstWindowMinutes : 15;
                            var threshold = settings.FailedLoginBurstThreshold > 0 ? settings.FailedLoginBurstThreshold : 10;
                            var recentFailed = await GetFailedLoginAttemptsAsync(ipAddress, withinMinutes);
                            if (recentFailed >= threshold)
                            {
                                var isPermanent = settings.AutoPermanentBlacklistEnabled;
                                var duration = settings.IpBlockDurationMinutes > 0 ? settings.IpBlockDurationMinutes : 30;
                                await BlockIpAsync(ipAddress, reason: $"Failed login burst: {recentFailed} in {withinMinutes}m", durationMinutes: duration, isPermanent: isPermanent);

                                if (isPermanent)
                                {
                                    await LogSecurityEventAsync(
                                        eventType: "IpBlacklistedPermanent",
                                        severity: "Critical",
                                        ipAddress: ipAddress,
                                        details: $"Auto-permanent blacklist due to failed login burst: {recentFailed}/{withinMinutes}m");
                                }
                            }
                        }
                    }
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

                    // Auto-block on failed login burst (fallback path)
                    if (eventType == "FailedLogin")
                    {
                        var settings = await _securitySettingsService.GetSettingsAsync();
                        if (settings.EnableIpBlocking)
                        {
                            var withinMinutes = settings.FailedLoginBurstWindowMinutes > 0 ? settings.FailedLoginBurstWindowMinutes : 15;
                            var threshold = settings.FailedLoginBurstThreshold > 0 ? settings.FailedLoginBurstThreshold : 10;
                            var recentFailed = await GetFailedLoginAttemptsAsync(ipAddress, withinMinutes);
                            if (recentFailed >= threshold)
                            {
                                var isPermanent = settings.AutoPermanentBlacklistEnabled;
                                var duration = settings.IpBlockDurationMinutes > 0 ? settings.IpBlockDurationMinutes : 30;
                                await BlockIpAsync(ipAddress, reason: $"Failed login burst: {recentFailed} in {withinMinutes}m", durationMinutes: duration, isPermanent: isPermanent);

                                if (isPermanent)
                                {
                                    await LogSecurityEventAsync(
                                        eventType: "IpBlacklistedPermanent",
                                        severity: "Critical",
                                        ipAddress: ipAddress,
                                        details: $"Auto-permanent blacklist due to failed login burst: {recentFailed}/{withinMinutes}m");
                                }
                            }
                        }
                    }
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

            // Optional email notification for IP blocking
            try
            {
                var settings = await _securitySettingsService.GetSettingsAsync();
                if (settings.NotifyOnIpBlocking && !string.IsNullOrWhiteSpace(settings.NotificationEmail))
                {
                    if (_emailService != null)
                    {
                        await SendSecurityAlertAsync(
                            settings.NotificationEmail,
                            "IP Address Temporarily Blocked",
                            $"IP Address: {ipAddress}\nReason: {reason}\nDuration: {durationMinutes} minutes\nExpires At: {DateTime.UtcNow.AddMinutes(durationMinutes):yyyy-MM-dd HH:mm:ss} UTC");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send IP block notification email");
            }

            // Re-block escalation: if frequent blocks within window, set permanent
            var blockSettings = await _securitySettingsService.GetSettingsAsync();
            if (blockSettings.AutoPermanentBlacklistEnabled)
            {
                var windowHours = blockSettings.ReblockWindowHours > 0 ? blockSettings.ReblockWindowHours : 24;
                var threshold = blockSettings.ReblockCountThreshold > 0 ? blockSettings.ReblockCountThreshold : 3;
                var cutoff = DateTime.UtcNow.AddHours(-windowHours);
                var recentBlocks = await _context.SecurityAuditLogs
                    .Where(l => l.IpAddress == ipAddress && l.EventType == "IpBlocked" && l.Timestamp >= cutoff)
                    .CountAsync();

                if (recentBlocks >= threshold)
                {
                    // Ensure permanent flag persisted
                    var blk = await _context.BlockedIps.FirstOrDefaultAsync(b => b.IpAddress == ipAddress);
                    if (blk != null)
                    {
                        blk.IsPermanent = true;
                        blk.ExpiresAt = null;
                        blk.Reason = $"{blk.Reason}; Escalated to permanent after {recentBlocks} blocks in {windowHours}h";
                        await _context.SaveChangesAsync();
                    }

                    await LogSecurityEventAsync(
                        eventType: "IpBlacklistedPermanent",
                        severity: "Critical",
                        ipAddress: ipAddress,
                        details: $"Auto-permanent blacklist due to re-block escalation: {recentBlocks} in {windowHours}h");

                    // Optional email notification for permanent blacklist
                    try
                    {
                        if (blockSettings.NotifyOnCriticalEvents && !string.IsNullOrWhiteSpace(blockSettings.NotificationEmail))
                        {
                            if (_emailService != null)
                            {
                                await SendSecurityAlertAsync(
                                    blockSettings.NotificationEmail,
                                    "⚠️ CRITICAL: IP Address Permanently Blacklisted",
                                    $"IP Address: {ipAddress}\nReason: Auto-escalation due to {recentBlocks} blocks within {windowHours} hours\nAction: Permanently blacklisted\nTimestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\nThis IP has been permanently blocked from accessing the system.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send permanent blacklist notification email");
                    }
                }
            }
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
            // Get all logs first
            var allLogs = await _context.SecurityAuditLogs
                .OrderByDescending(log => log.Timestamp)
                .Take(count * 2) // Get extra in case many are filtered out
                .ToListAsync();

            // Get whitelisted IPs
            var settings = await _securitySettingsService.GetSettingsAsync();
            var whitelistedIps = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(settings.WhitelistedIps))
            {
                whitelistedIps = settings.WhitelistedIps
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ip => ip.Trim())
                    .ToList();
            }

            // Filter out whitelisted IPs using CIDR matching
            var filteredLogs = allLogs.Where(log => 
            {
                if (string.IsNullOrEmpty(log.IpAddress))
                    return true;

                foreach (var whitelistedEntry in whitelistedIps)
                {
                    if (EcommerceStarter.Utilities.CIDRHelper.IsInCIDRRange(log.IpAddress, whitelistedEntry))
                        return false; // Exclude whitelisted IPs
                }
                return true;
            })
            .Take(count)
            .ToList();

            return filteredLogs;
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

        private async Task SendSecurityAlertAsync(string toEmail, string subject, string bodyText)
        {
            if (_emailService == null)
            {
                _logger.LogWarning("Cannot send security alert: Email service not configured");
                return;
            }

            try
            {
                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #dc3545; color: white; padding: 15px; border-radius: 5px 5px 0 0; }}
        .content {{ background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-top: none; }}
        .footer {{ padding: 15px; text-align: center; font-size: 12px; color: #6c757d; }}
        .alert-box {{ background: #fff3cd; border: 1px solid #ffc107; padding: 10px; margin: 15px 0; border-radius: 4px; }}
        pre {{ background: #e9ecef; padding: 10px; border-radius: 4px; overflow-x: auto; white-space: pre-wrap; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>🔒 Security Alert</h2>
        </div>
        <div class='content'>
            <h3>{subject}</h3>
            <div class='alert-box'>
                <strong>⚠️ Action Required:</strong> A security event has been triggered on your system.
            </div>
            <pre>{bodyText}</pre>
            <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p style='margin-top: 20px;'>
                Please review your security audit logs for more details.
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from EcommerceStarter.</p>
            <p>Do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

                await _emailService.SendTestEmailAsync(toEmail);
                _logger.LogInformation("Security alert notification sent to {Email} - Subject: {Subject}", toEmail, subject);
                _logger.LogInformation("Alert details: {Details}", bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security alert email to {Email}", toEmail);
                throw;
            }
        }
    }
}
