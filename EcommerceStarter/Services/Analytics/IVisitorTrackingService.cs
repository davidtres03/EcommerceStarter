using EcommerceStarter.Models.VisitorTracking;

namespace EcommerceStarter.Services.Analytics
{
    /// <summary>
    /// Interface for visitor tracking service
    /// </summary>
    public interface IVisitorTrackingService
    {
        /// <summary>
        /// Get or create a visitor session
        /// </summary>
        Task<VisitorSession> GetOrCreateSessionAsync(HttpContext context);
        
        /// <summary>
        /// Track a page view
        /// </summary>
        Task TrackPageViewAsync(int sessionId, string url, string? pageTitle, string? referrer);
        
        /// <summary>
        /// Track a custom event
        /// </summary>
        Task TrackEventAsync(int sessionId, string category, string action, string? label = null, decimal? value = null, string? metadata = null);
        
        /// <summary>
        /// Mark session as converted (made a purchase)
        /// </summary>
        Task MarkSessionAsConvertedAsync(int sessionId);
        
        /// <summary>
        /// Get analytics summary for a date range
        /// </summary>
        Task<AnalyticsSummary> GetAnalyticsSummaryAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Get top pages by views
        /// </summary>
        Task<List<PageStatistic>> GetTopPagesAsync(DateTime startDate, DateTime endDate, int count = 10);
        
        /// <summary>
        /// Get top referrers
        /// </summary>
        Task<List<ReferrerStatistic>> GetTopReferrersAsync(DateTime startDate, DateTime endDate, int count = 10);
        
        /// <summary>
        /// Get device type breakdown
        /// </summary>
        Task<Dictionary<string, int>> GetDeviceTypeBreakdownAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Get browser breakdown
        /// </summary>
        Task<Dictionary<string, int>> GetBrowserBreakdownAsync(DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Get recent events
        /// </summary>
        Task<List<VisitorEvent>> GetRecentEventsAsync(int count = 50);
    }
    
    /// <summary>
    /// Analytics summary data
    /// </summary>
    public class AnalyticsSummary
    {
        public int TotalSessions { get; set; }
        public int TotalPageViews { get; set; }
        public int UniqueVisitors { get; set; }
        public int ConvertedSessions { get; set; }
        public decimal ConversionRate { get; set; }
        public double AverageSessionDuration { get; set; }
        public double AveragePagesPerSession { get; set; }
        public int BounceRate { get; set; } // Sessions with only 1 page view
    }
    
    /// <summary>
    /// Page statistics
    /// </summary>
    public class PageStatistic
    {
        public string Url { get; set; } = string.Empty;
        public string? PageTitle { get; set; }
        public int Views { get; set; }
        public int UniqueVisitors { get; set; }
        public double AverageTimeOnPage { get; set; }
    }
    
    /// <summary>
    /// Referrer statistics
    /// </summary>
    public class ReferrerStatistic
    {
        public string Referrer { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public int Conversions { get; set; }
    }
}
