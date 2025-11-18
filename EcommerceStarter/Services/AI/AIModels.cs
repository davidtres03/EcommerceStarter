namespace EcommerceStarter.Services.AI;

/// <summary>
/// Represents a request to the AI backend
/// </summary>
public class AIRequest
{
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public string? RequestType { get; set; } // "chat", "code-generation", "review"
    public bool? PreferClaude { get; set; } // Override automatic routing
}

/// <summary>
/// Represents a response from the AI backend
/// </summary>
public class AIResponse
{
    public string Content { get; set; } = string.Empty;
    public string BackendUsed { get; set; } = string.Empty; // "Claude" or "Ollama"
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public decimal? EstimatedCost { get; set; } // For Claude API usage
    public int? TokensUsed { get; set; } // Total tokens (input + output) for Claude
}

/// <summary>
/// Represents a code modification request
/// </summary>
public class CodeModificationRequest
{
    public string Description { get; set; } = string.Empty;
    public List<string> AffectedFiles { get; set; } = new();
    public string? CurrentCode { get; set; }
    public string ProposedCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of applying a code modification
/// </summary>
public class ModificationResult
{
    public bool Success { get; set; }
    public string? CommitHash { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request routing decision
/// </summary>
public enum AIBackendType
{
    Claude,
    Ollama,
    Auto
}

/// <summary>
/// Analysis result for routing decisions
/// </summary>
public class RequestAnalysis
{
    public AIBackendType RecommendedBackend { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool RequiresCodeGeneration { get; set; }
    public bool IsComplexTask { get; set; }
    public decimal? EstimatedCost { get; set; }
}
