using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    public class SecurityAuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty; // LoginAttempt, FailedLogin, RateLimitExceeded, SuspiciousActivity, etc.

        [MaxLength(50)]
        public string? Severity { get; set; } // Low, Medium, High, Critical

        [MaxLength(255)]
        public string? UserId { get; set; }

        [MaxLength(255)]
        public string? UserEmail { get; set; }

        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        [MaxLength(255)]
        public string? Endpoint { get; set; }

        public bool IsBlocked { get; set; }
    }

    public class BlockedIp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        [Required]
        public DateTime BlockedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public int OffenseCount { get; set; } = 1;

        public bool IsPermanent { get; set; }
    }
}
