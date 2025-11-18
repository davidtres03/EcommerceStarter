namespace EcommerceStarter.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Services.AI;

/// <summary>
/// AI Control Panel API endpoints
/// Handles requests to the local AI assistants (Ollama and Claude)
/// Admin-only access required
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ILogger<AIController> _logger;

    public AIController(IAIService aiService, ILogger<AIController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message to the AI backend
    /// Routes intelligently between Ollama (free, local) and Claude (paid, cloud)
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> SendMessage([FromBody] AIRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Processing AI request - Type: {RequestType}", request.RequestType);
            var response = await _aiService.ProcessRequestAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning("AI request failed - Error: {Error}", response.ErrorMessage);
                return BadRequest(new { error = response.ErrorMessage });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing AI request");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Generate or modify code using AI
    /// </summary>
    [HttpPost("generate-code")]
    public async Task<IActionResult> GenerateCode([FromBody] CodeModificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Code generation requested - Files: {FileCount}", request.AffectedFiles.Count);

            var aiRequest = new AIRequest
            {
                Message = request.Description,
                Context = request.CurrentCode,
                RequestType = "code-generation",
                PreferClaude = true // Code generation prefers Claude
            };

            var response = await _aiService.ProcessRequestAsync(aiRequest);

            if (!response.Success)
            {
                return BadRequest(new { error = response.ErrorMessage });
            }

            // Return both the generated code and metadata
            return Ok(new
            {
                generatedCode = response.Content,
                backend = response.BackendUsed,
                estimatedCost = response.EstimatedCost,
                tokensUsed = response.TokensUsed,
                timestamp = response.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            return StatusCode(500, new { error = "Failed to generate code" });
        }
    }

    /// <summary>
    /// Check availability of configured AI backends
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var ollamaAvailable = await _aiService.IsOllamaAvailableAsync();
            var claudeAvailable = await _aiService.IsClaudeAvailableAsync();

            return Ok(new
            {
                ollama = ollamaAvailable,
                claude = claudeAvailable,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI backend status");
            return StatusCode(500, new { error = "Failed to check status" });
        }
    }
}
