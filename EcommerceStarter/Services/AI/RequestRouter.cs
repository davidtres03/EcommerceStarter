namespace EcommerceStarter.Services.AI;

/// <summary>
/// Analyzes requests and routes them to appropriate AI backend
/// </summary>
public interface IRequestRouter
{
    Task<RequestAnalysis> AnalyzeRequestAsync(AIRequest request);
    AIBackendType DetermineBackend(RequestAnalysis analysis, bool preferClaude = false);
}

public class RequestRouter : IRequestRouter
{
    private readonly ILogger<RequestRouter> _logger;

    // Keywords that indicate code generation is needed
    private static readonly string[] CodeGenerationKeywords = new[]
    {
        "add feature", "add page", "create", "modify code", "fix bug", "refactor",
        "change layout", "update", "implement", "build", "function", "class", "method",
        "component", "endpoint", "api", "database", "schema", "migration"
    };

    // Keywords that indicate complex tasks
    private static readonly string[] ComplexTaskKeywords = new[]
    {
        "architecture", "performance", "optimize", "security", "integrate", "migration",
        "refactor", "redesign", "scalability", "multi-file", "critical"
    };

    // Keywords for creative/simple tasks
    private static readonly string[] SimpleTaskKeywords = new[]
    {
        "explain", "tell me", "what is", "how do", "suggest", "brainstorm",
        "recommend", "help", "review", "analyze", "compare"
    };

    public RequestRouter(ILogger<RequestRouter> logger)
    {
        _logger = logger;
    }

    public async Task<RequestAnalysis> AnalyzeRequestAsync(AIRequest request)
    {
        var analysis = new RequestAnalysis();
        var messageLower = request.Message.ToLower();

        // Check if code generation is needed
        analysis.RequiresCodeGeneration = CodeGenerationKeywords
            .Any(keyword => messageLower.Contains(keyword));

        // Check if it's a complex task
        analysis.IsComplexTask = ComplexTaskKeywords
            .Any(keyword => messageLower.Contains(keyword));

        // Check if file paths are mentioned (strong indicator of code work)
        bool mentionsFiles = messageLower.Contains(".cs") ||
                            messageLower.Contains(".html") ||
                            messageLower.Contains(".css") ||
                            messageLower.Contains(".json") ||
                            messageLower.Contains("file") ||
                            messageLower.Contains("model") ||
                            messageLower.Contains("controller");

        analysis.RequiresCodeGeneration = analysis.RequiresCodeGeneration || mentionsFiles;

        // Estimate cost for Claude (approximately)
        if (analysis.RequiresCodeGeneration || analysis.IsComplexTask)
        {
            // Rough estimate: ~1000 tokens input + 2000 tokens output
            // Claude 3 Sonnet: $0.003 per 1K input, $0.015 per 1K output
            analysis.EstimatedCost = (1.0m * 0.003m) + (2.0m * 0.015m); // ~$0.035
        }
        else
        {
            analysis.EstimatedCost = 0.01m; // Simpler requests
        }

        // Determine reasoning
        if (analysis.RequiresCodeGeneration)
        {
            analysis.Reasoning = "Code generation detected - requires high-quality output";
            analysis.RecommendedBackend = AIBackendType.Claude;
        }
        else if (analysis.IsComplexTask)
        {
            analysis.Reasoning = "Complex task detected - Claude recommended for accuracy";
            analysis.RecommendedBackend = AIBackendType.Claude;
        }
        else
        {
            analysis.Reasoning = "Simple/creative task - Ollama suitable for cost savings";
            analysis.RecommendedBackend = AIBackendType.Ollama;
        }

        _logger.LogInformation(
            "Request analyzed - Backend: {Backend}, CodeGen: {CodeGen}, Complex: {Complex}, EstCost: ${Cost:F4}",
            analysis.RecommendedBackend,
            analysis.RequiresCodeGeneration,
            analysis.IsComplexTask,
            analysis.EstimatedCost ?? 0);

        return await Task.FromResult(analysis);
    }

    public AIBackendType DetermineBackend(RequestAnalysis analysis, bool preferClaude = false)
    {
        // User preference overrides everything
        if (preferClaude)
        {
            return AIBackendType.Claude;
        }

        // Otherwise use recommendation
        return analysis.RecommendedBackend;
    }
}
