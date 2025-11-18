using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStarter.Models;

/// <summary>
/// Represents an API service provider (Stripe, USPS, Claude, etc.)
/// Reference table for reusable provider information
/// </summary>
[Table("ApiProviders")]
public class ApiProvider
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique code for the provider (e.g., "Stripe", "USPS", "Claude")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the provider
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category: Payment, Shipping, AI, SMS, Email, etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Provider website or documentation URL
    /// </summary>
    [MaxLength(500)]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Base API endpoint (if applicable)
    /// </summary>
    [MaxLength(500)]
    public string? BaseEndpoint { get; set; }

    /// <summary>
    /// Whether this provider is currently supported/active in the system
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual ICollection<ApiSetting> Settings { get; set; } = new List<ApiSetting>();
}
