using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ErrorsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public List<ServiceErrorDto>? Errors { get; set; }
        public string? SeverityFilter { get; set; }
        public string? AcknowledgedFilter { get; set; }
        public int? HoursFilter { get; set; }
        public string? SourceFilter { get; set; }
        public int UnacknowledgedCount { get; set; }

        public ErrorsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync(string? severityFilter, string? acknowledgedFilter, int? hoursFilter, string? sourceFilter)
        {
            SeverityFilter = severityFilter;
            AcknowledgedFilter = acknowledgedFilter;
            HoursFilter = hoursFilter ?? 24;
            SourceFilter = sourceFilter;

            var query = _context.ServiceErrorLogs.AsQueryable();

            // Apply time filter
            var since = DateTime.UtcNow.AddHours(-(HoursFilter ?? 24));
            query = query.Where(e => e.Timestamp >= since);

            // Apply severity filter
            if (!string.IsNullOrEmpty(severityFilter))
            {
                query = query.Where(e => e.Severity == severityFilter);
            }

            // Apply acknowledgment filter
            if (acknowledgedFilter == "acknowledged")
            {
                query = query.Where(e => e.IsAcknowledged);
            }
            else if (acknowledgedFilter == "unacknowledged")
            {
                query = query.Where(e => !e.IsAcknowledged);
            }

            // Apply source filter
            if (!string.IsNullOrEmpty(sourceFilter))
            {
                query = query.Where(e => e.Source.Contains(sourceFilter));
            }

            // Get unacknowledged count
            UnacknowledgedCount = await _context.ServiceErrorLogs
                .Where(e => !e.IsAcknowledged && e.Timestamp >= since)
                .CountAsync();

            Errors = await query
                .OrderByDescending(e => e.Timestamp)
                .Take(500)
                .Select(e => new ServiceErrorDto
                {
                    Id = e.Id,
                    Timestamp = e.Timestamp,
                    Source = e.Source,
                    Severity = e.Severity,
                    Message = e.Message,
                    StackTrace = e.StackTrace,
                    IsAcknowledged = e.IsAcknowledged
                })
                .ToListAsync();
        }
    }
}
