namespace EcommerceStarter.Services.AI;

using EcommerceStarter.Data;
using EcommerceStarter.Models.AI;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Main AI service that orchestrates requests and routes to appropriate backends
/// </summary>
public class AIService : IAIService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIService> _logger;
    private readonly Dictionary<AIBackendType, IAIBackendService> _backends;

    public AIService(
        IServiceProvider serviceProvider,
        ILogger<AIService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _backends = new Dictionary<AIBackendType, IAIBackendService>();
    }

    /// <summary>
    /// Register a backend service
    /// </summary>
    public void RegisterBackend(AIBackendType type, IAIBackendService service)
    {
        _backends[type] = service;
        _logger.LogInformation("Registered AI backend: {Backend}", type);
    }

    /// <summary>
    /// Process an AI request with smart routing
    /// </summary>
    public async Task<AIResponse> ProcessRequestAsync(AIRequest request)
    {
        try
        {
            // Create a scope to resolve scoped services (Singleton can't directly inject Scoped)
            using var scope = _serviceProvider.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<IRequestRouter>();

            // Analyze the request
            var analysis = await router.AnalyzeRequestAsync(request);

            // Determine which backend to use
            var backendType = router.DetermineBackend(analysis, request.PreferClaude ?? false);

            // Get the appropriate backend
            if (!_backends.TryGetValue(backendType, out var backend))
            {
                // Try fallback to other backend
                var fallbackType = backendType == AIBackendType.Claude ? AIBackendType.Ollama : AIBackendType.Claude;

                if (!_backends.TryGetValue(fallbackType, out backend))
                {
                    _logger.LogError("No AI backend configured");
                    return new AIResponse
                    {
                        Success = false,
                        ErrorMessage = "No AI backend configured. Please set up Claude API key or Ollama endpoint.",
                        BackendUsed = "None"
                    };
                }

                _logger.LogWarning("Primary backend {Primary} not available, using {Fallback}",
                    backendType, fallbackType);
                backendType = fallbackType;
            }

            // Send to backend
            _logger.LogInformation("Routing request to {Backend}", backendType);
            var response = await backend.SendMessageAsync(request.Message, request.Context);
            response.BackendUsed = backendType.ToString();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI request");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"Error: {ex.Message}",
                BackendUsed = "Error"
            };
        }
    }

    /// <summary>
    /// Apply a code modification (placeholder for now)
    /// </summary>
    public async Task<ModificationResult> ApplyCodeModificationAsync(CodeModificationRequest modification, string commitMessage)
    {
        try
        {
            _logger.LogInformation("Applying code modification: {Description}", modification.Description);

            // PHASE 2: Implement file modification logic
            // 1. Validate files exist and are writable
            // 2. Create backup in temp folder
            // 3. Apply code changes from modification.ProposedCode
            // 4. Run git commit with provided message
            // 5. Log modification to AIModificationLog table
            // Note: This requires integration with git CLI and file system operations

            return await Task.FromResult(new ModificationResult
            {
                Success = true,
                CommitHash = "placeholder", // Will be replaced with real git hash
                ModifiedFiles = modification.AffectedFiles,
                AppliedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying code modification");
            return new ModificationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Get chat history (placeholder)
    /// </summary>
    public async Task<List<string>> GetChatHistoryAsync(string userId, int limit = 50)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var history = await dbContext.AIChatHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(limit)
                .Select(h => $"[{h.CreatedAt:g}] User: {h.UserMessage}\nAI ({h.BackendUsed}): {h.AIResponse}\n")
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} chat history records for user {UserId}", history.Count, userId);
            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history for user {UserId}", userId);
            return new List<string>();
        }
    }

    /// <summary>
    /// Save interaction to database (placeholder)
    /// </summary>
    public async Task SaveInteractionAsync(string userId, AIRequest request, AIResponse response, AIBackendType backend)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var interaction = new AIChatHistory
            {
                UserId = userId,
                UserMessage = request.Message,
                AIResponse = response.Content,
                BackendUsed = backend.ToString(),
                RequestType = request.RequestType ?? "chat",
                EstimatedCost = response.EstimatedCost,
                TokensUsed = response.TokensUsed,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.AIChatHistories.AddAsync(interaction);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Saved AI interaction - User: {UserId}, Backend: {Backend}, Type: {Type}, Cost: ${Cost}",
                userId, backend, request.RequestType, response.EstimatedCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving interaction for user {UserId}", userId);
            // Don't throw - interaction failure shouldn't break the chat
        }
    }

    /// <summary>
    /// Check if Ollama backend is available
    /// </summary>
    public async Task<bool> IsOllamaAvailableAsync()
    {
        if (!_backends.TryGetValue(AIBackendType.Ollama, out var backend))
        {
            return false;
        }

        // Use a simple message to test availability
        var response = await backend.SendMessageAsync("test");
        return response.Success;
    }

    /// <summary>
    /// Check if Claude backend is available
    /// </summary>
    public async Task<bool> IsClaudeAvailableAsync()
    {
        if (!_backends.TryGetValue(AIBackendType.Claude, out var backend))
        {
            return false;
        }

        // Use a simple message to test availability
        var response = await backend.SendMessageAsync("test");
        return response.Success;
    }
}
