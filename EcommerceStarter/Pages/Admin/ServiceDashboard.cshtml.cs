using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcommerceStarter.Models.Service;
using EcommerceStarter.Data;
using EcommerceStarter.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ServiceDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimezoneService _timezoneService;

        public ServiceStatusDto? CurrentStatus { get; set; }
        public List<UpdateHistoryDto>? RecentUpdates { get; set; }
        public List<ServiceErrorDto>? RecentErrors { get; set; }
        public PerformanceMetricsDto? Metrics { get; set; }

        public ServiceDashboardModel(ApplicationDbContext context, ITimezoneService timezoneService)
        {
            _context = context;
            _timezoneService = timezoneService;
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Get current status
                var latestStatus = await _context.ServiceStatusLogs
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                if (latestStatus != null)
                {
                    CurrentStatus = new ServiceStatusDto
                    {
                        Timestamp = _timezoneService.ConvertUtcToLocalTime(latestStatus.Timestamp),
                        IsWebServiceOnline = latestStatus.IsWebServiceOnline,
                        ResponseTimeMs = latestStatus.ResponseTimeMs,
                        IsBackgroundServiceRunning = latestStatus.IsBackgroundServiceRunning,
                        PendingOrdersCount = latestStatus.PendingOrdersCount,
                        MemoryUsageMb = latestStatus.MemoryUsageMb,
                        CpuUsagePercent = Math.Round(latestStatus.CpuUsagePercent, 2),
                        DatabaseConnected = latestStatus.DatabaseConnected,
                        ActiveUserCount = latestStatus.ActiveUserCount,
                        QueueSize = latestStatus.QueueSize,
                        UptimePercent = Math.Round(latestStatus.UptimePercent, 2),
                        ErrorMessage = latestStatus.ErrorMessage
                    };
                }

                // Get recent updates
                var updates = await _context.UpdateHistories
                    .OrderByDescending(u => u.AppliedAt)
                    .Take(10)
                    .ToListAsync();

                RecentUpdates = updates.Select(u => new UpdateHistoryDto
                {
                    Id = u.Id,
                    Version = u.Version,
                    AppliedAt = _timezoneService.ConvertUtcToLocalTime(u.AppliedAt),
                    Status = u.Status,
                    ReleaseNotes = u.ReleaseNotes,
                    ErrorMessage = u.ErrorMessage,
                    ApplyDurationSeconds = u.ApplyDurationSeconds
                }).ToList();

                // Get recent errors
                var errors = await _context.ServiceErrorLogs
                    .Where(e => !e.IsAcknowledged)
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10)
                    .ToListAsync();

                RecentErrors = errors.Select(e => new ServiceErrorDto
                {
                    Id = e.Id,
                    Timestamp = _timezoneService.ConvertUtcToLocalTime(e.Timestamp),
                    Source = e.Source,
                    Severity = e.Severity,
                    Message = e.Message,
                    IsAcknowledged = e.IsAcknowledged
                }).ToList();

                // Get performance metrics (last 24 hours)
                var last24Hours = DateTime.UtcNow.AddHours(-24);
                var statusLogs = await _context.ServiceStatusLogs
                    .Where(s => s.Timestamp >= last24Hours)
                    .ToListAsync();

                if (statusLogs.Any())
                {
                    Metrics = new PerformanceMetricsDto
                    {
                        AverageResponseTimeMs = (int)statusLogs.Average(s => s.ResponseTimeMs),
                        AverageCpuUsagePercent = Math.Round((decimal)statusLogs.Average(s => (double)s.CpuUsagePercent), 2),
                        AverageMemoryUsageMb = (int)statusLogs.Average(s => s.MemoryUsageMb),
                        UptimePercent = Math.Round((decimal)statusLogs.Average(s => s.UptimePercent), 2)
                    };
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("Invalid object name"))
            {
                // Service monitoring tables don't exist yet - migration needs to be run
                // Initialize empty collections so the page doesn't crash
                CurrentStatus = null;
                RecentUpdates = new List<UpdateHistoryDto>();
                RecentErrors = new List<ServiceErrorDto>();
                Metrics = null;
            }
        }
    }

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
        public int QueueSize { get; set; }
        public decimal UptimePercent { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class UpdateHistoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public string? ErrorMessage { get; set; }
        public int ApplyDurationSeconds { get; set; }
    }

    public class ServiceErrorDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public bool IsAcknowledged { get; set; }
    }

    public class PerformanceMetricsDto
    {
        public int AverageResponseTimeMs { get; set; }
        public decimal AverageCpuUsagePercent { get; set; }
        public int AverageMemoryUsageMb { get; set; }
        public decimal UptimePercent { get; set; }
    }
}
