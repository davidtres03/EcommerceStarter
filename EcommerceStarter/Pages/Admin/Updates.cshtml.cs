using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcommerceStarter.Data;
using EcommerceStarter.Pages.Admin;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UpdatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public List<UpdateHistoryDto>? Updates { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public UpdatesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync(string? statusFilter, DateTime? fromDate, DateTime? toDate)
        {
            StatusFilter = statusFilter;
            FromDate = fromDate;
            ToDate = toDate;

            var query = _context.UpdateHistories.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(u => u.Status == statusFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(u => u.AppliedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(u => u.AppliedAt <= toDate.Value.AddDays(1));
            }

            Updates = await query
                .OrderByDescending(u => u.AppliedAt)
                .Take(100)
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
        }
    }
}
