using System;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models.Service
{
    /// <summary>
    /// Tracks service status and health metrics over time
    /// </summary>
    public class ServiceStatusLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp of the status check
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Is the main web service online
        /// </summary>
        public bool IsWebServiceOnline { get; set; }

        /// <summary>
        /// Last response time in milliseconds
        /// </summary>
        public int ResponseTimeMs { get; set; }

        /// <summary>
        /// Is the background service running
        /// </summary>
        public bool IsBackgroundServiceRunning { get; set; }

        /// <summary>
        /// Number of pending orders
        /// </summary>
        public int PendingOrdersCount { get; set; }

        /// <summary>
        /// Current memory usage in MB
        /// </summary>
        public int MemoryUsageMb { get; set; }

        /// <summary>
        /// CPU usage percentage (0-100)
        /// </summary>
        public decimal CpuUsagePercent { get; set; }

        /// <summary>
        /// Database connection status
        /// </summary>
        public bool DatabaseConnected { get; set; }

        /// <summary>
        /// Number of active users
        /// </summary>
        public int ActiveUserCount { get; set; }

        /// <summary>
        /// Current queue size for analytics/audit events
        /// </summary>
        public int QueueSize { get; set; }

        /// <summary>
        /// Any error or warning message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Overall system uptime percentage
        /// </summary>
        public decimal UptimePercent { get; set; }
    }

    /// <summary>
    /// Tracks update history
    /// </summary>
    public class UpdateHistory
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Version number that was installed
        /// </summary>
        [Required]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// When the update was applied
        /// </summary>
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// Status of the update (Success, Failed, Rolled Back)
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty; // "Success", "Failed", "RolledBack"

        /// <summary>
        /// Release notes for this version
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// Any error message if update failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Time taken to apply update in seconds
        /// </summary>
        public int ApplyDurationSeconds { get; set; }
    }

    /// <summary>
    /// Tracks error logs from services
    /// </summary>
    public class ServiceErrorLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// When the error occurred
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Source of the error (AI Service, Background Service, Web API, etc.)
        /// </summary>
        [Required]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Severity level (Info, Warning, Error, Critical)
        /// </summary>
        [Required]
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Error message
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace for debugging
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// Whether this error has been acknowledged by admin
        /// </summary>
        public bool IsAcknowledged { get; set; }

        /// <summary>
        /// When the error was acknowledged
        /// </summary>
        public DateTime? AcknowledgedAt { get; set; }
    }

    /// <summary>
    /// DTO for displaying service status on monitoring dashboard
    /// </summary>
    public class ServiceStatusDto
    {
        public DateTime Timestamp { get; set; }
        public bool IsWebServiceOnline { get; set; }
        public int ResponseTimeMs { get; set; }
        public bool IsBackgroundServiceRunning { get; set; }
        public int PendingOrdersCount { get; set; }
        public int MemoryUsageMb { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public bool DatabaseConnected { get; set; }
        public int ActiveUserCount { get; set; }
        public decimal UptimePercent { get; set; }
        public string? ErrorMessage { get; set; }
        public List<UpdateHistoryDto> RecentUpdates { get; set; } = new();
        public List<ServiceErrorDto> RecentErrors { get; set; } = new();
    }

    /// <summary>
    /// DTO for update history display
    /// </summary>
    public class UpdateHistoryDto
    {
        public string Version { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public int ApplyDurationSeconds { get; set; }
    }

    /// <summary>
    /// DTO for error log display
    /// </summary>
    public class ServiceErrorDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsAcknowledged { get; set; }
    }

    /// <summary>
    /// Performance metrics summary for dashboard
    /// </summary>
    public class PerformanceMetricsDto
    {
        public decimal AverageResponseTimeMs { get; set; }
        public decimal AverageMemoryUsageMb { get; set; }
        public decimal AverageCpuUsagePercent { get; set; }
        public decimal UptimePercent { get; set; }
        public int TotalErrors24Hours { get; set; }
        public int CriticalErrors24Hours { get; set; }
        public int UpdatesApplied30Days { get; set; }
        public DateTime? LastSuccessfulBackup { get; set; }
    }

    /// <summary>
    /// DTO for receiving service status logs from Windows Service
    /// </summary>
    public class ServiceStatusLogDto
    {
        public bool IsWebServiceOnline { get; set; }
        public int ResponseTimeMs { get; set; }
        public bool IsBackgroundServiceRunning { get; set; }
        public int PendingOrdersCount { get; set; }
        public int MemoryUsageMb { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public bool DatabaseConnected { get; set; }
        public int ActiveUserCount { get; set; }
        public int QueueSize { get; set; }
        public decimal UptimePercent { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// DTO for receiving service error logs from Windows Service
    /// </summary>
    public class ServiceErrorLogDto
    {
        public string Source { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// DTO for receiving update history logs from Windows Service
    /// </summary>
    public class UpdateHistoryLogDto
    {
        public string Version { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public string? ErrorMessage { get; set; }
        public int ApplyDurationSeconds { get; set; }
    }
}
