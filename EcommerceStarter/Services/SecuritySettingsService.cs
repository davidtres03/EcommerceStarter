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
        Task<bool> ShouldIgnoreIpForAnalyticsAsync(string ipAddress);
        Task<AnalyticsIgnoreReason> GetAnalyticsIgnoreReasonAsync(string ipAddress);
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
                existing.AutoPermanentBlacklistEnabled = settings.AutoPermanentBlacklistEnabled;
                existing.ErrorSpikeThresholdPerMinute = settings.ErrorSpikeThresholdPerMinute;
                existing.ErrorSpikeConsecutiveMinutes = settings.ErrorSpikeConsecutiveMinutes;
                existing.ReblockCountThreshold = settings.ReblockCountThreshold;
                existing.ReblockWindowHours = settings.ReblockWindowHours;
                existing.FailedLoginBurstThreshold = settings.FailedLoginBurstThreshold;
                existing.FailedLoginBurstWindowMinutes = settings.FailedLoginBurstWindowMinutes;
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

            var whitelistedEntries = settings.WhitelistedIps
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim());

            foreach (var entry in whitelistedEntries)
            {
                // Support both single IPs and CIDR notation
                if (EcommerceStarter.Utilities.CIDRHelper.IsInCIDRRange(ipAddress, entry))
                    return true;
            }

            return false;
        }

        public async Task<bool> IsIpBlacklistedAsync(string ipAddress)
        {
            var settings = await GetSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.BlacklistedIps))
                return false;

            var blacklistedEntries = settings.BlacklistedIps
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim());

            foreach (var entry in blacklistedEntries)
            {
                // Support both single IPs and CIDR notation
                if (EcommerceStarter.Utilities.CIDRHelper.IsInCIDRRange(ipAddress, entry))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if an IP should be ignored for analytics tracking and geolocation.
        /// Uses the same WhitelistedIps list - trusted IPs bypass security AND don't pollute analytics.
        /// </summary>
        public async Task<bool> ShouldIgnoreIpForAnalyticsAsync(string ipAddress)
        {
            var reason = await GetAnalyticsIgnoreReasonAsync(ipAddress);
            return reason != AnalyticsIgnoreReason.None;
        }

        public async Task<AnalyticsIgnoreReason> GetAnalyticsIgnoreReasonAsync(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || IsNonPublicOrLocalIp(ipAddress))
                return AnalyticsIgnoreReason.PrivateOrLocal;

            if (await IsIpWhitelistedAsync(ipAddress))
                return AnalyticsIgnoreReason.Whitelisted;

            return AnalyticsIgnoreReason.None;
        }

        private bool IsNonPublicOrLocalIp(string ipAddress)
        {
            // Localhost and loopback
            if (ipAddress.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
                ipAddress.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                ipAddress.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                ipAddress.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                return true;

            // Quickly handle common private/link-local ranges for IPv4
            if (ipAddress.StartsWith("10.") ||
                ipAddress.StartsWith("192.168.") ||
                ipAddress.StartsWith("172.16.") || ipAddress.StartsWith("172.17.") || ipAddress.StartsWith("172.18.") || ipAddress.StartsWith("172.19.") ||
                ipAddress.StartsWith("172.20.") || ipAddress.StartsWith("172.21.") || ipAddress.StartsWith("172.22.") || ipAddress.StartsWith("172.23.") ||
                ipAddress.StartsWith("172.24.") || ipAddress.StartsWith("172.25.") || ipAddress.StartsWith("172.26.") || ipAddress.StartsWith("172.27.") ||
                ipAddress.StartsWith("172.28.") || ipAddress.StartsWith("172.29.") || ipAddress.StartsWith("172.30.") || ipAddress.StartsWith("172.31.") ||
                ipAddress.StartsWith("169.254."))
                return true;

            // IPv6: link-local and unique local addresses
            if (ipAddress.StartsWith("fe80:", StringComparison.OrdinalIgnoreCase) ||
                ipAddress.StartsWith("fc00:", StringComparison.OrdinalIgnoreCase) ||
                ipAddress.StartsWith("fd", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }

    public enum AnalyticsIgnoreReason
    {
        None = 0,
        Whitelisted = 1,
        PrivateOrLocal = 2
    }
}
