namespace EcommerceStarter.Models.VisitorTracking
{
    /// <summary>
    /// Represents custom events tracked for analytics
    /// (e.g., product views, add to cart, search queries)
    /// </summary>
    public class VisitorEvent
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to VisitorSession
        /// </summary>
        public int SessionId { get; set; }
        
        /// <summary>
        /// Event category (e.g., "E-commerce", "Search", "Navigation")
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Event action (e.g., "Product View", "Add to Cart", "Search")
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        /// <summary>
        /// Event label (e.g., product name, search query)
        /// </summary>
        public string? Label { get; set; }
        
        /// <summary>
        /// Numeric value (e.g., product price, quantity)
        /// </summary>
        public decimal? Value { get; set; }
        
        /// <summary>
        /// Additional metadata as JSON
        /// </summary>
        public string? Metadata { get; set; }
        
        /// <summary>
        /// Timestamp of event
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Page URL where event occurred
        /// </summary>
        public string? PageUrl { get; set; }
        
        /// <summary>
        /// Navigation to parent session
        /// </summary>
        public virtual VisitorSession Session { get; set; } = null!;
    }
}
