using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    public interface ISecuritySettingsService
    {
        Task<SecuritySettings> GetSettingsAsync();
        Task UpdateSettingsAsync(SecuritySettings settings, string modifiedBy);
        Task<bool> IsIpWhitelistedAsync(string ipAddress);
        Task<bool> IsIpBlacklistedAsync(string ipAddress);
    }

    public class SecuritySettingsService : ISecuritySettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SecuritySettingsService> _logger;
        private SecuritySettings? _cachedSettings;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
        private readonly object _cacheLock = new();

        public SecuritySettingsService(ApplicationDbContext context, ILogger<SecuritySettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SecuritySettings> GetSettingsAsync()
        {
            lock (_cacheLock)
            {
                if (_cachedSettings != null && DateTime.UtcNow < _cacheExpiry)
                {
                    return _cachedSettings;
                }
            }

            var settings = await _context.SecuritySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings
                settings = new SecuritySettings();
                _context.SecuritySettings.Add(settings);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created default security settings");
            }

            lock (_cacheLock)
            {
                _cachedSettings = settings;
                _cacheExpiry = DateTime.UtcNow.Add(_cacheDuration);
            }

            return settings;
        }

        public async Task UpdateSettingsAsync(SecuritySettings settings, string modifiedBy)
        {
            var existing = await _context.SecuritySettings.FirstOrDefaultAsync();

            if (existing == null)
            {
                _context.SecuritySettings.Add(settings);
            }
            else
            {
                existing.MaxRequestsPerMinute = settings.MaxRequestsPerMinute;
                existing.MaxRequestsPerSecond = settings.MaxRequestsPerSecond;
                existing.MaxRequestsPerMinuteAuth = settings.MaxRequestsPerMinuteAuth;
                existing.MaxRequestsPerSecondAuth = settings.MaxRequestsPerSecondAuth;
                existing.EnableRateLimiting = settings.EnableRateLimiting;
                existing.ExemptAdminsFromRateLimiting = settings.ExemptAdminsFromRateLimiting;
                existing.MaxFailedLoginAttempts = settings.MaxFailedLoginAttempts;
                existing.FailedLoginWindowMinutes = settings.FailedLoginWindowMinutes;
                existing.IpBlockDurationMinutes = settings.IpBlockDurationMinutes;
                existing.EnableIpBlocking = settings.EnableIpBlocking;
                existing.AccountLockoutMaxAttempts = settings.AccountLockoutMaxAttempts;
                existing.AccountLockoutDurationMinutes = settings.AccountLockoutDurationMinutes;
                existing.EnableAccountLockout = settings.EnableAccountLockout;
                existing.EnableSecurityAuditLogging = settings.EnableSecurityAuditLogging;
                existing.AuditLogRetentionDays = settings.AuditLogRetentionDays;
                existing.NotifyOnCriticalEvents = settings.NotifyOnCriticalEvents;
                existing.NotifyOnIpBlocking = settings.NotifyOnIpBlocking;
                existing.NotificationEmail = settings.NotificationEmail;
                existing.EnableGeoIpBlocking = settings.EnableGeoIpBlocking;
                existing.BlockedCountries = settings.BlockedCountries;
                existing.WhitelistedIps = settings.WhitelistedIps;
                existing.BlacklistedIps = settings.BlacklistedIps;
                existing.LastModified = DateTime.UtcNow;
                existing.LastModifiedBy = modifiedBy;
            }

            await _context.SaveChangesAsync();

            // Invalidate cache
            lock (_cacheLock)
            {
                _cachedSettings = null;
                _cacheExpiry = DateTime.MinValue;
            }

            _logger.LogInformation("Security settings updated by {User}", modifiedBy);
        }

        public async Task<bool> IsIpWhitelistedAsync(string ipAddress)
        {
            var settings = await GetSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.WhitelistedIps))
                return false;

            var whitelistedIps = settings.WhitelistedIps
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim());

            return whitelistedIps.Contains(ipAddress);
        }

        public async Task<bool> IsIpBlacklistedAsync(string ipAddress)
        {
            var settings = await GetSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.BlacklistedIps))
                return false;

            var blacklistedIps = settings.BlacklistedIps
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim());

            return blacklistedIps.Contains(ipAddress);
        }
    }
}
