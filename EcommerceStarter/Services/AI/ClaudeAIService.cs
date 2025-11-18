namespace EcommerceStarter.Services.AI;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Claude AI backend service - Uses Anthropic Claude API via HTTP
/// Implements intelligent code generation and general-purpose AI tasks
/// </summary>
public class ClaudeAIService : IAIBackendService
{
    private readonly ILogger<ClaudeAIService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private string? _apiKey;
    private string? _model;
    private int _maxTokens;
    private DateTime _configLoadedAt = DateTime.MinValue;
    private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromSeconds(30); // Reduced for faster config updates

    // Claude 3 Sonnet pricing (as of Nov 2024)
    private const decimal InputCostPer1kTokens = 0.003m;
    private const decimal OutputCostPer1kTokens = 0.015m;
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";

    public string BackendName => "Claude";
    public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

    public ClaudeAIService(
        IServiceScopeFactory serviceScopeFactory,
        IEncryptionService encryptionService,
        ILogger<ClaudeAIService> logger, 
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        _encryptionService = encryptionService;

        // Defaults
        _model = "claude-3-5-sonnet-20241022";
        _maxTokens = 2000;

        _logger.LogInformation("Claude service initialized - will load configuration from database on first use");
    }

    /// <summary>
    /// Force reload configuration from database (bypass cache)
    /// </summary>
    public void InvalidateCache()
    {
        _configLoadedAt = DateTime.MinValue;
        _logger.LogInformation("Claude cache invalidated - will reload on next request");
    }

    private async Task EnsureConfigurationLoadedAsync()
    {
        // Only reload if cache expired or not loaded yet
        if (DateTime.UtcNow - _configLoadedAt < ConfigCacheDuration)
            return;

        try
        {
            // Create scope to resolve DbContext (Scoped service can't be injected into Scoped service stored by Singleton)
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var claudeConfig = await context.ApiConfigurations
                .AsNoTracking()
                .Where(ac => ac.ApiType == "Claude" && ac.IsActive)
                .FirstOrDefaultAsync();

            if (claudeConfig != null && !string.IsNullOrEmpty(claudeConfig.EncryptedValue1))
            {
                // Decrypt API key
                _apiKey = _encryptionService.Decrypt(claudeConfig.EncryptedValue1);

                // Load metadata if available (case-insensitive property names)
                if (!string.IsNullOrEmpty(claudeConfig.MetadataJson))
                {
                    var metadata = JsonSerializer.Deserialize<JsonElement>(claudeConfig.MetadataJson);
                    
                    // Try both lowercase and PascalCase
                    if (metadata.TryGetProperty("model", out var modelProp) ||
                        metadata.TryGetProperty("Model", out modelProp))
                        _model = modelProp.GetString() ?? _model;
                        
                    if (metadata.TryGetProperty("maxTokens", out var tokensProp) ||
                        metadata.TryGetProperty("MaxTokens", out tokensProp))
                        _maxTokens = tokensProp.GetInt32();

                    // Check if enabled
                    bool enabled = true; // Default to enabled for Claude
                    if (metadata.TryGetProperty("enabled", out var enabledProp) ||
                        metadata.TryGetProperty("Enabled", out enabledProp))
                        enabled = enabledProp.GetBoolean();

                    if (!enabled)
                    {
                        _logger.LogWarning("Claude configuration found but not enabled");
                        _apiKey = null;
                    }

                    _logger.LogInformation("Claude configuration loaded - Model: {Model}, Enabled: {Enabled}", _model, enabled);
                }
                else
                {
                    _logger.LogInformation("Claude configuration loaded - Model: {Model} (default)", _model);
                }

                _configLoadedAt = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning("No active Claude configuration found in ApiConfigurations table");
                _apiKey = null;
                _configLoadedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Claude configuration from database");
            _apiKey = null;
        }
    }

    public async Task<AIResponse> SendMessageAsync(string message, string? context = null)
    {
        try
        {
            // Ensure configuration is loaded (uses 5-minute cache)
            await EnsureConfigurationLoadedAsync();
            
            if (!IsConfigured)
            {
                _logger.LogWarning("Claude API key not configured");
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "Claude API key not configured",
                    BackendUsed = BackendName
                };
            }

            var systemPrompt = context ?? "You are a helpful AI assistant. Provide clear, concise responses.";

            _logger.LogInformation("Sending message to Claude API - Model: {Model}", _model);

            var request = new ClaudeMessageRequest
            {
                Model = _model,
                MaxTokens = _maxTokens,
                System = systemPrompt,
                Messages = new List<ClaudeMessage>
                {
                    new ClaudeMessage { Role = "user", Content = message }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseJson = await response.Content.ReadAsStringAsync();
            var responseContent = JsonSerializer.Deserialize<ClaudeMessageResponse>(responseJson);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {StatusCode}", response.StatusCode);
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"Claude API returned {response.StatusCode}",
                    BackendUsed = BackendName
                };
            }

            var responseText = responseContent?.Content?.FirstOrDefault()?.Text ?? "Unable to parse response";
            var inputTokens = responseContent?.Usage?.InputTokens ?? 0;
            var outputTokens = responseContent?.Usage?.OutputTokens ?? 0;
            var estimatedCost = (inputTokens / 1000m * InputCostPer1kTokens) +
                               (outputTokens / 1000m * OutputCostPer1kTokens);

            _logger.LogInformation(
                "Claude response - Input: {InputTokens}, Output: {OutputTokens}, Cost: ${EstimatedCost:F4}",
                inputTokens, outputTokens, estimatedCost);

            return new AIResponse
            {
                Content = responseText,
                BackendUsed = BackendName,
                Success = true,
                EstimatedCost = estimatedCost,
                TokensUsed = inputTokens + outputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"Claude error: {ex.Message}",
                BackendUsed = BackendName
            };
        }
    }

    public async Task<AIResponse> GenerateCodeAsync(string description, string? currentCode = null)
    {
        var systemPrompt = """
            You are an expert C# and web development assistant specializing in ASP.NET Core.
            Generate production-quality code that follows best practices:
            - Include comprehensive XML documentation comments
            - Use proper error handling and validation
            - Follow C# naming conventions (PascalCase for public members)
            - Focus on clarity, performance, and maintainability
            - Use async/await patterns for I/O operations
            - Include logging where appropriate

            When modifying existing code, preserve functionality and improve gradually.
            Always consider security implications.
            """;

        var userMessage = currentCode != null
            ? $"""
                I need to modify this existing C# code:

                ```csharp
                {currentCode}
                ```

                Please modify it to: {description}

                Return ONLY the modified code in a code block, no explanation needed.
                """
            : $"""
                Please generate C# code for the following requirement:

                {description}

                Return ONLY the code in a code block, no explanation needed.
                Context: This is for an ASP.NET Core 8.0 Razor Pages web application.
                """;

        try
        {
            // Ensure configuration is loaded (uses 5-minute cache)
            await EnsureConfigurationLoadedAsync();
            
            if (!IsConfigured)
            {
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "Claude API key not configured",
                    BackendUsed = BackendName
                };
            }

            _logger.LogInformation("Generating code via Claude - Description: {Description}", description);

            var request = new ClaudeMessageRequest
            {
                Model = _model,
                MaxTokens = _maxTokens,
                System = systemPrompt,
                Messages = new List<ClaudeMessage>
                {
                    new ClaudeMessage { Role = "user", Content = userMessage }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(httpRequest);
            var responseJson = await response.Content.ReadAsStringAsync();
            var responseContent = JsonSerializer.Deserialize<ClaudeMessageResponse>(responseJson);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {StatusCode}", response.StatusCode);
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"Claude API returned {response.StatusCode}",
                    BackendUsed = BackendName
                };
            }

            var generatedCode = responseContent?.Content?.FirstOrDefault()?.Text ?? "Unable to parse generated code";
            var inputTokens = responseContent?.Usage?.InputTokens ?? 0;
            var outputTokens = responseContent?.Usage?.OutputTokens ?? 0;
            var estimatedCost = (inputTokens / 1000m * InputCostPer1kTokens) +
                               (outputTokens / 1000m * OutputCostPer1kTokens);

            _logger.LogInformation(
                "Code generation complete - Input: {InputTokens}, Output: {OutputTokens}, Cost: ${EstimatedCost:F4}",
                inputTokens, outputTokens, estimatedCost);

            return new AIResponse
            {
                Content = generatedCode,
                BackendUsed = BackendName,
                Success = true,
                EstimatedCost = estimatedCost,
                TokensUsed = inputTokens + outputTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code via Claude");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = $"Code generation error: {ex.Message}",
                BackendUsed = BackendName
            };
        }
    }

    /// <summary>
    /// Tests whether Claude API is accessible and responding
    /// </summary>
    public async Task<bool> IsClaudeAvailableAsync()
    {
        try
        {
            // Ensure configuration is loaded (uses 5-minute cache)
            await EnsureConfigurationLoadedAsync();
            
            if (!IsConfigured)
            {
                _logger.LogWarning("Claude not configured");
                return false;
            }

            var request = new ClaudeMessageRequest
            {
                Model = _model,
                MaxTokens = 10,
                Messages = new List<ClaudeMessage>
                {
                    new ClaudeMessage { Role = "user", Content = "ok" }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("x-api-key", _apiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.SendAsync(httpRequest);
            _logger.LogInformation("Claude availability check: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude availability check failed");
            return false;
        }
    }
}

// Helper classes for Claude API communication
internal class ClaudeMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class ClaudeMessageRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("messages")]
    public List<ClaudeMessage> Messages { get; set; } = new();
}

internal class ClaudeContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal class ClaudeUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}

internal class ClaudeMessageResponse
{
    [JsonPropertyName("content")]
    public List<ClaudeContentBlock>? Content { get; set; }

    [JsonPropertyName("usage")]
    public ClaudeUsage? Usage { get; set; }
}
