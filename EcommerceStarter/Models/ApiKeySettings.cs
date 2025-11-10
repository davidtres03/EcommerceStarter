using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Stores API credentials for third-party carrier tracking services
    /// Keys are encrypted before storage
    /// </summary>
    public class ApiKeySettings
    {
        [Key]
        public int Id { get; set; }

        // USPS Web Tools API
        [MaxLength(255)]
        public string? UspsUserId { get; set; }

        [MaxLength(500)]
        public string? UspsPasswordEncrypted { get; set; }

        public bool UspsEnabled { get; set; } = false;

        public bool UspsUseSandbox { get; set; } = false; // Toggle between sandbox and production environments

        // UPS Developer API
        [MaxLength(255)]
        public string? UpsClientId { get; set; }

        [MaxLength(500)]
        public string? UpsClientSecretEncrypted { get; set; }

        [MaxLength(255)]
        public string? UpsAccountNumber { get; set; }

        public bool UpsEnabled { get; set; } = false;

        // FedEx Web Services
        [MaxLength(255)]
        public string? FedExAccountNumber { get; set; }

        [MaxLength(255)]
        public string? FedExMeterNumber { get; set; }

        [MaxLength(500)]
        public string? FedExKeyEncrypted { get; set; }

        [MaxLength(500)]
        public string? FedExPasswordEncrypted { get; set; }

        public bool FedExEnabled { get; set; } = false;

        // Metadata
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string? LastUpdatedBy { get; set; }
    }
}
