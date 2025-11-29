namespace EcommerceStarter.Models
{
    /// <summary>
    /// Represents an audit log entry for customer activities
    /// </summary>
    public class CustomerAuditLog
    {
        public int Id { get; set; }
        
        /// <summary>
        /// ID of the customer this log entry belongs to
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;
        
        /// <summary>
        /// Navigation property to the customer
        /// </summary>
        public virtual ApplicationUser Customer { get; set; } = null!;
        
        /// <summary>
        /// Type of event (Login, EmailSent, PasswordReset, etc.)
        /// </summary>
        public string EventType { get; set; } = string.Empty;
        
        /// <summary>
        /// Category of the event (Authentication, Email, Account, Security)
        /// </summary>
        public AuditEventCategory Category { get; set; }
        
        /// <summary>
        /// Brief description of the event
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Additional details stored as JSON (e.g., email subject, IP address, etc.)
        /// </summary>
        public string? Details { get; set; }
        
        /// <summary>
        /// IP address from which the action was performed (if applicable)
        /// </summary>
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// User agent/browser information (if applicable)
        /// </summary>
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// When this event occurred
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success { get; set; } = true;
        
        /// <summary>
        /// Error message if the action failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// Categories for audit events
    /// </summary>
    public enum AuditEventCategory
    {
        /// <summary>
        /// Login, logout, failed login attempts
        /// </summary>
        Authentication = 0,
        
        /// <summary>
        /// Emails sent to the customer
        /// </summary>
        Email = 1,
        
        /// <summary>
        /// Account changes (email, password, profile updates)
        /// </summary>
        Account = 2,
        
        /// <summary>
        /// Security events (password reset, email verification)
        /// </summary>
        Security = 3,
        
        /// <summary>
        /// Admin actions performed on the customer account
        /// </summary>
        AdminAction = 4
    }
}
