using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Unified API configuration model for all external services
    /// Stores encrypted credentials for Stripe, Cloudinary, USPS, UPS, FedEx, Claude, Ollama, etc.
    /// </summary>
    public class ApiConfiguration
    {
        public int Id { get; set; }

        /// <summary>
        /// Type of API (Stripe, Cloudinary, USPS, UPS, FedEx, Claude, Ollama)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ApiType { get; set; } = string.Empty;

        /// <summary>
        /// Name/Environment identifier (e.g., "Stripe-Live", "Stripe-Test", "Cloudinary-Main", "USPS-Production")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether this configuration is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this is a test/sandbox environment
        /// </summary>
        public bool IsTestMode { get; set; } = false;

        // Encrypted fields - generic storage for all credential types
        [MaxLength(1000)]
        public string? EncryptedValue1 { get; set; }  // Primary credential (PublishableKey, CloudName, ConsumerKey, etc.)

        [MaxLength(1000)]
        public string? EncryptedValue2 { get; set; }  // Secondary credential (SecretKey, ApiKey, ConsumerSecret, etc.)

        [MaxLength(1000)]
        public string? EncryptedValue3 { get; set; }  // Tertiary credential (WebhookSecret, MeterNumber, etc.)

        [MaxLength(1000)]
        public string? EncryptedValue4 { get; set; }  // Quaternary credential (additional field if needed)

        [MaxLength(1000)]
        public string? EncryptedValue5 { get; set; }  // Quinary credential (for flexibility)

        /// <summary>
        /// Additional metadata stored as JSON for configuration-specific settings
        /// Example: {"maxTokens": 2000, "model": "claude-3-5-sonnet-20241022", "endpoint": "http://localhost:11434"}
        /// </summary>
        [MaxLength(5000)]
        public string? MetadataJson { get; set; }

        /// <summary>
        /// Human-readable description of what these credentials are for
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// When this configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this configuration was last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who created this configuration
        /// </summary>
        [MaxLength(450)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// User ID who last updated this configuration
        /// </summary>
        [MaxLength(450)]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// When this configuration was last validated/tested
        /// </summary>
        public DateTime? LastValidated { get; set; }

        /// <summary>
        /// Related audit logs
        /// </summary>
        public ICollection<ApiConfigurationAuditLog> AuditLogs { get; set; } = new List<ApiConfigurationAuditLog>();

        /// <summary>
        /// Helper method for masking Stripe Publishable Key (EncryptedValue1)
        /// </summary>
        public string GetMaskedPublishableKey()
        {
            if (string.IsNullOrEmpty(EncryptedValue1))
                return "Not Set";

            return MaskKey(IsTestMode ? "pk_test" : "pk_live");
        }

        /// <summary>
        /// Helper method for masking Stripe Secret Key (EncryptedValue2)
        /// </summary>
        public string GetMaskedSecretKey()
        {
            if (string.IsNullOrEmpty(EncryptedValue2))
                return "Not Set";

            return MaskKey(IsTestMode ? "sk_test" : "sk_live");
        }

        /// <summary>
        /// Helper method for masking Stripe Webhook Secret (EncryptedValue3)
        /// </summary>
        public string GetMaskedWebhookSecret()
        {
            if (string.IsNullOrEmpty(EncryptedValue3))
                return "Not Set";

            return "whsec_****";
        }

        private static string MaskKey(string prefix)
        {
            return $"{prefix}_****";
        }
    }

    /// <summary>
    /// Audit log for tracking all changes to API configurations
    /// </summary>
    public class ApiConfigurationAuditLog
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to ApiConfiguration
        /// </summary>
        public int ApiConfigurationId { get; set; }

        /// <summary>
        /// Navigation property to ApiConfiguration
        /// </summary>
        public ApiConfiguration? ApiConfiguration { get; set; }

        /// <summary>
        /// Type of action (Created, Updated, Deleted, Viewed, Tested, Activated, Deactivated)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// What fields were changed and how (stored as JSON)
        /// </summary>
        [MaxLength(5000)]
        public string? Changes { get; set; }

        /// <summary>
        /// Timestamp of the audit log
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who performed the action
        /// </summary>
        [MaxLength(450)]
        public string? UserId { get; set; }

        /// <summary>
        /// User email/name for easier tracking
        /// </summary>
        [MaxLength(256)]
        public string? UserEmail { get; set; }

        /// <summary>
        /// IP address from which the action was performed
        /// </summary>
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// If this was an API test, the result status
        /// </summary>
        [MaxLength(20)]
        public string? TestStatus { get; set; }  // "SUCCESS", "FAILED", "WARNING"

        /// <summary>
        /// Any notes about the test or operation
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
