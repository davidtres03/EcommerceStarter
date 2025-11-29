using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Stores encrypted SSL/TLS certificate and private key for production deployment
    /// </summary>
    public class SslConfiguration
    {
        public int Id { get; set; }

        /// <summary>
        /// Encrypted SSL certificate (PEM format)
        /// </summary>
        [Required]
        public string EncryptedCertificate { get; set; } = string.Empty;

        /// <summary>
        /// Encrypted private key (PEM format)
        /// </summary>
        [Required]
        public string EncryptedPrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Domain name this certificate is for
        /// </summary>
        [Required]
        public string DomainName { get; set; } = string.Empty;

        /// <summary>
        /// Certificate issuer (e.g., "Cloudflare", "Let's Encrypt")
        /// </summary>
        public string Issuer { get; set; } = "Unknown";

        /// <summary>
        /// Certificate expiration date
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// When the certificate was added to the database
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time the certificate was updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who updated the certificate
        /// </summary>
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// Whether this certificate is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Helper methods
        public string GetMaskedCertificate()
        {
            if (string.IsNullOrEmpty(EncryptedCertificate))
                return "Not Set";

            return $"Certificate for {DomainName} (expires: {ExpirationDate?.ToShortDateString() ?? "Unknown"})";
        }

        public string GetMaskedPrivateKey()
        {
            if (string.IsNullOrEmpty(EncryptedPrivateKey))
                return "Not Set";

            return "Private Key (encrypted)";
        }

        public bool IsExpiringSoon(int daysThreshold = 30)
        {
            if (!ExpirationDate.HasValue)
                return false;

            return (ExpirationDate.Value - DateTime.UtcNow).TotalDays <= daysThreshold;
        }

        public bool IsExpired()
        {
            if (!ExpirationDate.HasValue)
                return false;

            return ExpirationDate.Value < DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Audit log for SSL certificate changes
    /// </summary>
    public class SslConfigurationAuditLog
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? UserId { get; set; }

        public string? UserEmail { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Viewed", "Exported", "Deleted"

        public string? DomainName { get; set; }

        public string? Changes { get; set; } // JSON of what changed

        public string? IpAddress { get; set; }

        public string? Notes { get; set; }
    }
}
