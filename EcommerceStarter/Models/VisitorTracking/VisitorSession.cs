namespace EcommerceStarter.Models.VisitorTracking
{
    /// <summary>
    /// Represents a unique visitor session
    /// </summary>
    public class VisitorSession
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Unique session identifier (GUID)
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Visitor's IP address (anonymized for privacy)
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// User agent string
        /// </summary>
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// Browser name (parsed from user agent)
        /// </summary>
        public string? Browser { get; set; }
        public string? BrowserVersion { get; set; }
        
        /// <summary>
        /// Device type (Desktop, Mobile, Tablet)
        /// </summary>
        public string? DeviceType { get; set; }
        
        /// <summary>
        /// Operating system
        /// </summary>
        public string? OperatingSystem { get; set; }
        public string? OSVersion { get; set; }
        public string? DeviceBrand { get; set; }
        public string? DeviceModel { get; set; }
        public bool IsBot { get; set; }
        public string? BotName { get; set; }
        
        /// <summary>
        /// Referrer URL (where visitor came from)
        /// </summary>
        public string? Referrer { get; set; }
        
        /// <summary>
        /// Landing page URL
        /// </summary>
        public string? LandingPage { get; set; }
        
        /// <summary>
        /// Session start time
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Last activity time
        /// </summary>
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Session end time (null if still active)
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Total page views in this session (count)
        /// </summary>
        public int PageViewCount { get; set; } = 0;
        
        /// <summary>
        /// User ID if logged in (nullable)
        /// </summary>
        public string? UserId { get; set; }
        
        /// <summary>
        /// Country (from IP geolocation - optional)
        /// </summary>
        public string? Country { get; set; }
        
        /// <summary>
        /// City (from IP geolocation - optional)
        /// </summary>
        public string? City { get; set; }
        
        /// <summary>
        /// Whether visitor converted (made a purchase)
        /// </summary>
        public bool Converted { get; set; } = false;
        
        /// <summary>
        /// Navigation to related page views
        /// </summary>
        public virtual ICollection<PageView> PageViews { get; set; } = new List<PageView>();
        
        /// <summary>
        /// Navigation to related events
        /// </summary>
        public virtual ICollection<VisitorEvent> Events { get; set; } = new List<VisitorEvent>();
    }
}
