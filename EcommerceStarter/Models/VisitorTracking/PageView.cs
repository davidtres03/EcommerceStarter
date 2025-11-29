namespace EcommerceStarter.Models.VisitorTracking
{
    /// <summary>
    /// Represents a page view within a visitor session
    /// </summary>
    public class PageView
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to VisitorSession
        /// </summary>
        public int SessionId { get; set; }
        
        /// <summary>
        /// Page URL
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Page title
        /// </summary>
        public string? PageTitle { get; set; }
        
        /// <summary>
        /// Referrer URL (previous page)
        /// </summary>
        public string? Referrer { get; set; }
        
        /// <summary>
        /// Timestamp of page view
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Time spent on page (in seconds) - calculated when user navigates away
        /// </summary>
        public int? TimeOnPage { get; set; }
        
        /// <summary>
        /// Query string parameters (for analytics)
        /// </summary>
        public string? QueryString { get; set; }
        
        /// <summary>
        /// Navigation to parent session
        /// </summary>
        public virtual VisitorSession Session { get; set; } = null!;
    }
}
