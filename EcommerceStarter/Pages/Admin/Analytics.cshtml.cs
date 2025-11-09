using EcommerceStarter.Services.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsModel : PageModel
    {
        private readonly IVisitorTrackingService _trackingService;
        private readonly ILogger<AnalyticsModel> _logger;

        public AnalyticsModel(
            IVisitorTrackingService trackingService,
            ILogger<AnalyticsModel> logger)
        {
            _trackingService = trackingService;
            _logger = logger;
        }

        public AnalyticsSummary Summary { get; set; } = new();
        public List<PageStatistic> TopPages { get; set; } = new();
        public List<ReferrerStatistic> TopReferrers { get; set; } = new();
        public Dictionary<string, int> DeviceBreakdown { get; set; } = new();
        public Dictionary<string, int> BrowserBreakdown { get; set; } = new();
        public List<EcommerceStarter.Models.VisitorTracking.VisitorEvent> RecentEvents { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string DateRange { get; set; } = "7days";

        public async Task OnGetAsync()
        {
            try
            {
                var (startDate, endDate) = GetDateRange();

                Summary = await _trackingService.GetAnalyticsSummaryAsync(startDate, endDate);
                TopPages = await _trackingService.GetTopPagesAsync(startDate, endDate, 10);
                TopReferrers = await _trackingService.GetTopReferrersAsync(startDate, endDate, 10);
                DeviceBreakdown = await _trackingService.GetDeviceTypeBreakdownAsync(startDate, endDate);
                BrowserBreakdown = await _trackingService.GetBrowserBreakdownAsync(startDate, endDate);
                RecentEvents = await _trackingService.GetRecentEventsAsync(20);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics data");
                TempData["Error"] = "Error loading analytics data. Please try again.";
            }
        }

        private (DateTime startDate, DateTime endDate) GetDateRange()
        {
            var endDate = DateTime.UtcNow;
            var startDate = DateRange switch
            {
                "today" => DateTime.UtcNow.Date,
                "yesterday" => DateTime.UtcNow.Date.AddDays(-1),
                "7days" => DateTime.UtcNow.AddDays(-7),
                "30days" => DateTime.UtcNow.AddDays(-30),
                "90days" => DateTime.UtcNow.AddDays(-90),
                "year" => DateTime.UtcNow.AddYears(-1),
                _ => DateTime.UtcNow.AddDays(-7)
            };

            return (startDate, endDate);
        }
    }
}
