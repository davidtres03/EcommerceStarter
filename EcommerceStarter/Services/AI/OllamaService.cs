namespace EcommerceStarter.Services.AI;

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ollama AI backend service - Uses local Ollama LLM
/// Free, runs locally, good for creative tasks and simple queries
/// </summary>
public class OllamaService : IAIBackendService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private string? _endpoint;
    private string? _model;
    private DateTime _configLoadedAt = DateTime.MinValue;
    private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromSeconds(30); // Reduced for faster config updates

    public string BackendName => "Ollama";
    public bool IsConfigured => !string.IsNullOrEmpty(_endpoint);

    public OllamaService(
        ILogger<OllamaService> logger, 
        HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _httpClient = httpClient;
        _serviceScopeFactory = serviceScopeFactory;
        
        _logger.LogInformation("Ollama service initialized - will load configuration from database on first use");
    }

    /// <summary>
    /// Force reload configuration from database (bypass cache)
    /// </summary>
    public void InvalidateCache()
    {
        _configLoadedAt = DateTime.MinValue;
        _logger.LogInformation("Ollama cache invalidated - will reload on next request");
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
            
            // Get active Ollama configuration from ApiConfigurations table
            var ollamaConfig = await context.ApiConfigurations
                .AsNoTracking()
                .Where(ac => ac.ApiType == "Ollama" && ac.IsActive)
                .FirstOrDefaultAsync();

            if (ollamaConfig != null && !string.IsNullOrEmpty(ollamaConfig.MetadataJson))
            {
                // Parse metadata to get endpoint and model (case-insensitive property names)
                var metadata = JsonSerializer.Deserialize<JsonElement>(ollamaConfig.MetadataJson);
                
                // Try both lowercase and PascalCase
                if (metadata.TryGetProperty("endpoint", out var endpointProp) || 
                    metadata.TryGetProperty("Endpoint", out endpointProp))
                    _endpoint = endpointProp.GetString();
                    
                if (metadata.TryGetProperty("model", out var modelProp) ||
                    metadata.TryGetProperty("Model", out modelProp))
                    _model = modelProp.GetString();

                // Check if enabled
                bool enabled = false;
                if (metadata.TryGetProperty("enabled", out var enabledProp) ||
                    metadata.TryGetProperty("Enabled", out enabledProp))
                    enabled = enabledProp.GetBoolean();

                if (!enabled)
                {
                    _logger.LogWarning("Ollama configuration found but not enabled");
                    _endpoint = null;
                    _model = null;
                }

                _configLoadedAt = DateTime.UtcNow;
                _logger.LogInformation("Ollama configuration loaded - Endpoint: {Endpoint}, Model: {Model}, Enabled: {Enabled}", 
                    _endpoint ?? "not set", _model ?? "not set", enabled);
            }
            else
            {
                _logger.LogWarning("No active Ollama configuration found in ApiConfigurations table");
                _endpoint = null;
                _model = null;
                _configLoadedAt = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Ollama configuration from database");
            _endpoint = null;
            _model = null;
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
                _logger.LogWarning("Ollama service not configured");
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = "Ollama endpoint not configured. Please configure Ollama in API Settings.",
                    BackendUsed = BackendName
                };
            }

            if (!await IsOllamaAvailableAsync())
            {
                _logger.LogWarning("Ollama not available at {Endpoint}", _endpoint);
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"Ollama not available at {_endpoint}. Please ensure Ollama is running.",
                    BackendUsed = BackendName
                };
            }

            var prompt = context != null
                ? $"{context}\n\nUser: {message}"
                : message;

            var request = new OllamaRequest
            {
                Model = _model ?? "llama2",
                Prompt = prompt,
                Stream = false
            };

            _logger.LogInformation("Sending message to Ollama - Model: {Model}", _model);

            var response = await _httpClient.PostAsJsonAsync($"{_endpoint}/api/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Ollama error: {StatusCode}", response.StatusCode);
                return new AIResponse
                {
                    Success = false,
                    ErrorMessage = $"Ollama error: {response.StatusCode}",
                    BackendUsed = BackendName
                };
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            _logger.LogInformation("Ollama response received - Model: {Model}", _model);

            return new AIResponse
            {
                Content = result?.Response ?? "No response",
                BackendUsed = BackendName,
                Success = true,
                EstimatedCost = 0 // Ollama is free/local
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama API");
            return new AIResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                BackendUsed = BackendName
            };
        }
    }

    public async Task<AIResponse> GenerateCodeAsync(string description, string? currentCode = null)
    {
        var codeContext = currentCode != null
            ? $"Current code:\n```\n{currentCode}\n```\n\n"
            : "";

        var prompt = $"{codeContext}Please generate code for: {description}";

        return await SendMessageAsync(prompt);
    }

    private async Task<bool> IsOllamaAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_endpoint}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Helper classes for Ollama API
    private class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
