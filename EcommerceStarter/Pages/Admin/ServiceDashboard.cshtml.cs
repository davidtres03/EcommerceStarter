using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcommerceStarter.Models.Service;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ServiceDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ServiceStatusDto? CurrentStatus { get; set; }
        public List<UpdateHistoryDto>? RecentUpdates { get; set; }
        public List<ServiceErrorDto>? RecentErrors { get; set; }
        public PerformanceMetricsDto? Metrics { get; set; }

        public ServiceDashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            // Get current status
            var latestStatus = await _context.ServiceStatusLogs
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();

            if (latestStatus != null)
            {
                CurrentStatus = new ServiceStatusDto
                {
                    Timestamp = latestStatus.Timestamp,
                    IsWebServiceOnline = latestStatus.IsWebServiceOnline,
                    ResponseTimeMs = latestStatus.ResponseTimeMs,
                    IsBackgroundServiceRunning = latestStatus.IsBackgroundServiceRunning,
                    MemoryUsageMb = latestStatus.MemoryUsageMb,
                    CpuUsagePercent = latestStatus.CpuUsagePercent,
                    DatabaseConnected = latestStatus.DatabaseConnected,
                    UptimePercent = latestStatus.UptimePercent
                };
            }

            // Get recent updates
            RecentUpdates = await _context.UpdateHistories
                .OrderByDescending(u => u.AppliedAt)
                .Take(10)
                .Select(u => new UpdateHistoryDto
                {
                    Id = u.Id,
                    Version = u.Version,
                    AppliedAt = u.AppliedAt,
                    Status = u.Status,
                    ReleaseNotes = u.ReleaseNotes,
                    ErrorMessage = u.ErrorMessage,
                    ApplyDurationSeconds = u.ApplyDurationSeconds
                })
                .ToListAsync();

            // Get recent errors
            RecentErrors = await _context.ServiceErrorLogs
                .Where(e => !e.IsAcknowledged)
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
                    AverageCpuUsagePercent = (decimal)statusLogs.Average(s => (double)s.CpuUsagePercent),
                    AverageMemoryUsageMb = (int)statusLogs.Average(s => s.MemoryUsageMb),
                    UptimePercent = (decimal)statusLogs.Average(s => s.UptimePercent)
                };
            }
        }
    }

    public class ServiceStatusDto
    {
        public DateTime Timestamp { get; set; }
        public bool IsWebServiceOnline { get; set; }
        public int ResponseTimeMs { get; set; }
        public bool IsBackgroundServiceRunning { get; set; }
        public int MemoryUsageMb { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public bool DatabaseConnected { get; set; }
        public int ActiveUserCount { get; set; }
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
