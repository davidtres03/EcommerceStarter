using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    public class SecuritySettings
    {
        [Key]
        public int Id { get; set; }

        // Rate Limiting Settings
        [Range(1, 1000)]
        public int MaxRequestsPerMinute { get; set; } = 60;

        [Range(1, 100)]
        public int MaxRequestsPerSecond { get; set; } = 20;

        [Range(1, 100)]
        public int MaxRequestsPerMinuteAuth { get; set; } = 10;

        [Range(1, 50)]
        public int MaxRequestsPerSecondAuth { get; set; } = 3;

        public bool EnableRateLimiting { get; set; } = true;

        public bool ExemptAdminsFromRateLimiting { get; set; } = true;

        // IP Blocking Settings
        [Range(1, 100)]
        public int MaxFailedLoginAttempts { get; set; } = 5;

        [Range(1, 1440)]
        public int FailedLoginWindowMinutes { get; set; } = 15;

        [Range(1, 10080)]
        public int IpBlockDurationMinutes { get; set; } = 30;

        public bool EnableIpBlocking { get; set; } = true;

        // Auto-Blacklist Settings
        public bool AutoPermanentBlacklistEnabled { get; set; } = true;

        [Range(1, 1000)]
        public int ErrorSpikeThresholdPerMinute { get; set; } = 20; // errors/min to trigger

        [Range(1, 60)]
        public int ErrorSpikeConsecutiveMinutes { get; set; } = 1; // consecutive minutes required

        [Range(1, 100)]
        public int ReblockCountThreshold { get; set; } = 3; // number of re-blocks to escalate

        [Range(1, 168)]
        public int ReblockWindowHours { get; set; } = 24; // window to count re-blocks

        [Range(1, 100)]
        public int FailedLoginBurstThreshold { get; set; } = 10; // failed logins to trigger

        [Range(1, 1440)]
        public int FailedLoginBurstWindowMinutes { get; set; } = 15;

        // Account Lockout Settings
        [Range(1, 100)]
        public int AccountLockoutMaxAttempts { get; set; } = 5;

        [Range(1, 1440)]
        public int AccountLockoutDurationMinutes { get; set; } = 15;

        public bool EnableAccountLockout { get; set; } = true;

        // Audit Logging Settings
        public bool EnableSecurityAuditLogging { get; set; } = true;

        [Range(1, 365)]
        public int AuditLogRetentionDays { get; set; } = 90;

        // Security Event Notifications
        public bool NotifyOnCriticalEvents { get; set; } = true;

        public bool NotifyOnIpBlocking { get; set; } = true;

        [MaxLength(500)]
        public string? NotificationEmail { get; set; }

        // Advanced Settings
        public bool EnableGeoIpBlocking { get; set; } = false;

        [MaxLength(2000)]
        public string? BlockedCountries { get; set; } // Comma-separated ISO country codes

        [MaxLength(2000)]
        public string? WhitelistedIps { get; set; } // Comma-separated IP addresses

        [MaxLength(2000)]
        public string? BlacklistedIps { get; set; } // Comma-separated IP addresses permanently blocked

        [Required]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? LastModifiedBy { get; set; }
    }
}
