namespace EcommerceStarter.Services.AI;

/// <summary>
/// Interface for AI backend services
/// </summary>
public interface IAIBackendService
{
    Task<AIResponse> SendMessageAsync(string message, string? context = null);
    Task<AIResponse> GenerateCodeAsync(string description, string? currentCode = null);
    string BackendName { get; }
    bool IsConfigured { get; }
}

/// <summary>
/// Orchestrates AI operations with routing and logging
/// </summary>
public interface IAIService
{
    Task<AIResponse> ProcessRequestAsync(AIRequest request);
    Task<ModificationResult> ApplyCodeModificationAsync(CodeModificationRequest modification, string commitMessage);
    Task<List<string>> GetChatHistoryAsync(string userId, int limit = 50);
    Task SaveInteractionAsync(string userId, AIRequest request, AIResponse response, AIBackendType backend);
    Task<bool> IsOllamaAvailableAsync();
    Task<bool> IsClaudeAvailableAsync();
    void RegisterBackend(AIBackendType type, IAIBackendService service);
}
