using EcommerceStarter.Models;

namespace EcommerceStarter.Models.Tracking
{
    /// <summary>
    /// Real-time tracking status from carrier API
    /// </summary>
    public class TrackingStatus
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public Courier Courier { get; set; }
        
        /// <summary>
        /// Current delivery status
        /// </summary>
        public string CurrentStatus { get; set; } = "Unknown";
        
        /// <summary>
        /// Standardized status for UI display
        /// </summary>
        public TrackingStatusType StatusType { get; set; } = TrackingStatusType.Unknown;
        
        /// <summary>
        /// Last known location
        /// </summary>
        public string? LastLocation { get; set; }
        
        /// <summary>
        /// City where package currently is
        /// </summary>
        public string? City { get; set; }
        
        /// <summary>
        /// State where package currently is
        /// </summary>
        public string? State { get; set; }
        
        /// <summary>
        /// When status was last updated by carrier
        /// </summary>
        public DateTime? LastUpdate { get; set; }
        
        /// <summary>
        /// Estimated delivery date/time
        /// </summary>
        public DateTime? EstimatedDelivery { get; set; }
        
        /// <summary>
        /// Actual delivery date/time (if delivered)
        /// </summary>
        public DateTime? DeliveredAt { get; set; }
        
        /// <summary>
        /// Who signed for delivery (if applicable)
        /// </summary>
        public string? SignedBy { get; set; }
        
        /// <summary>
        /// Timeline of all tracking events
        /// </summary>
        public List<TrackingEvent> Events { get; set; } = new();
        
        /// <summary>
        /// True if package was delivered
        /// </summary>
        public bool IsDelivered => StatusType == TrackingStatusType.Delivered;
        
        /// <summary>
        /// True if there's an exception/problem
        /// </summary>
        public bool HasException => StatusType == TrackingStatusType.Exception;
    }

    /// <summary>
    /// Individual tracking event from carrier
    /// </summary>
    public class TrackingEvent
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Standardized tracking status types across all carriers
    /// </summary>
    public enum TrackingStatusType
    {
        Unknown = 0,
        Pending = 1,
        PickedUp = 2,
        InTransit = 3,
        OutForDelivery = 4,
        Delivered = 5,
        Exception = 6,
        Returned = 7
    }
}
