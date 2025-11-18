using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcommerceStarter.Data;
using EcommerceStarter.Models.Service;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class MetricsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PerformanceMetricsDto? Metrics { get; set; }
        public List<StatusLogDto>? StatusLogs { get; set; }

        public MetricsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            // Get last 24 hours of data
            var last24Hours = DateTime.UtcNow.AddHours(-24);
            var statusLogs = await _context.ServiceStatusLogs
                .Where(s => s.Timestamp >= last24Hours)
                .OrderByDescending(s => s.Timestamp)
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

                StatusLogs = statusLogs
                    .Select(s => new StatusLogDto
                    {
                        Timestamp = s.Timestamp,
                        ResponseTimeMs = s.ResponseTimeMs,
                        CpuUsagePercent = s.CpuUsagePercent,
                        MemoryUsageMb = s.MemoryUsageMb,
                        IsWebServiceOnline = s.IsWebServiceOnline,
                        IsBackgroundServiceRunning = s.IsBackgroundServiceRunning,
                        DatabaseConnected = s.DatabaseConnected,
                        ActiveUserCount = s.ActiveUserCount
                    })
                    .ToList();
            }
        }
    }

    public class StatusLogDto
    {
        public DateTime Timestamp { get; set; }
        public int ResponseTimeMs { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public int MemoryUsageMb { get; set; }
        public bool IsWebServiceOnline { get; set; }
        public bool IsBackgroundServiceRunning { get; set; }
        public bool DatabaseConnected { get; set; }
        public int ActiveUserCount { get; set; }
    }
}
