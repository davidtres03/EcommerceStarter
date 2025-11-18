using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    public class StripeConfiguration
    {
        public int Id { get; set; }

        [Required]
        public string EncryptedPublishableKey { get; set; } = string.Empty;

        [Required]
        public string EncryptedSecretKey { get; set; } = string.Empty;

        [Required]
        public string EncryptedWebhookSecret { get; set; } = string.Empty;

        public bool IsTestMode { get; set; } = true;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        public string? UpdatedBy { get; set; }

        // Helper methods for masking (not stored in DB)
        public string GetMaskedPublishableKey()
        {
            if (string.IsNullOrEmpty(EncryptedPublishableKey))
                return "Not Set";

            return MaskKey(IsTestMode ? "pk_test" : "pk_live");
        }

        public string GetMaskedSecretKey()
        {
            if (string.IsNullOrEmpty(EncryptedSecretKey))
                return "Not Set";

            return MaskKey(IsTestMode ? "sk_test" : "sk_live");
        }

        public string GetMaskedWebhookSecret()
        {
            if (string.IsNullOrEmpty(EncryptedWebhookSecret))
                return "Not Set";

            return "whsec_****";
        }

        private string MaskKey(string prefix)
        {
            return $"{prefix}_****";
        }
    }

    public class StripeConfigurationAuditLog
    {
        public int Id { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public string? UserId { get; set; }
        
        public string? UserEmail { get; set; }
        
        [Required]
        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Viewed"
        
        public string? Changes { get; set; } // JSON of what changed
        
        public string? IpAddress { get; set; }
        
        public bool WasTestMode { get; set; }
    }
}
