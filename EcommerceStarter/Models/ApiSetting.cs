using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStarter.Models;

/// <summary>
/// Represents a single API configuration setting
/// One row per setting for normalized storage
/// </summary>
[Table("ApiSettings")]
public class ApiSetting
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApiProvider
    /// </summary>
    [Required]
    public int ApiProviderId { get; set; }

    /// <summary>
    /// Setting key (e.g., "PublishableKey", "SecretKey", "UserId", "Endpoint")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SettingKey { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted value (for sensitive data like API keys, passwords)
    /// </summary>
    [MaxLength(2000)]
    public string? EncryptedValue { get; set; }

    /// <summary>
    /// Plain text value (for non-sensitive data like endpoints, usernames, model names)
    /// </summary>
    [MaxLength(500)]
    public string? PlainValue { get; set; }

    /// <summary>
    /// Data type hint: String, Int, Bool, Decimal, Json
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ValueType { get; set; } = "String";

    /// <summary>
    /// Whether this is a test/sandbox setting
    /// </summary>
    public bool IsTestMode { get; set; } = false;

    /// <summary>
    /// Whether this setting is currently active/enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Optional description or notes
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Display order for UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// When this setting was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this setting was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last updated this setting
    /// </summary>
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// When this setting was last validated/tested
    /// </summary>
    public DateTime? LastValidated { get; set; }

    // Navigation property
    [ForeignKey("ApiProviderId")]
    public virtual ApiProvider? Provider { get; set; }
}
