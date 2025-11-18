namespace EcommerceStarter.Models.AI;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Stores AI system configuration and settings
/// </summary>
public class AdminAIConfig
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string SettingKey { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? SettingValue { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Records all AI chat interactions for history and audit
/// </summary>
public class AIChatHistory
{
    public int Id { get; set; }

    [Required]
    [StringLength(450)] // Match IdentityUser Id length
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string UserMessage { get; set; } = string.Empty;

    [Required]
    [StringLength(int.MaxValue)]
    public string AIResponse { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string BackendUsed { get; set; } = string.Empty; // "Claude" or "Ollama"

    [StringLength(50)]
    public string? RequestType { get; set; } // "chat", "code-generation", etc.

    [Column(TypeName = "decimal(10,2)")]
    public decimal? EstimatedCost { get; set; }
    public int? TokensUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Logs all code modifications made through AI assistant
/// </summary>
public class AIModificationLog
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(int.MaxValue)]
    public string? ProposedCode { get; set; }

    [StringLength(int.MaxValue)]
    public string? PreviousCode { get; set; }

    [StringLength(500)]
    public string? FilePath { get; set; }

    [StringLength(100)]
    public string? CommitHash { get; set; }

    public bool Applied { get; set; } = false;
    public bool Rolled { get; set; } = false;

    [StringLength(1000)]
    public string? RollbackReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AppliedAt { get; set; }
    public DateTime? RolledBackAt { get; set; }
}
