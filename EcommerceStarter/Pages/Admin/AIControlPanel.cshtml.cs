using EcommerceStarter.Data;
using EcommerceStarter.Models.AI;
using EcommerceStarter.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AIControlPanelModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<AIControlPanelModel> _logger;

        public AIControlPanelModel(
            ApplicationDbContext context,
            IAIService aiService,
            ILogger<AIControlPanelModel> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        [BindProperty]
        public string UserMessage { get; set; } = string.Empty;

        public List<AIChatHistory> ChatHistory { get; set; } = new();
        public string? LastResponse { get; set; }
        public string? LastBackendUsed { get; set; }
        public decimal? LastEstimatedCost { get; set; }
        public int? LastTokensUsed { get; set; }
        public string? ResponseTime { get; set; }
        public string? ErrorMessage { get; set; }

        // Statistics
        public int TotalInteractions { get; set; }
        public decimal TotalCost { get; set; }
        public int TotalTokensUsed { get; set; }
        public List<string> AvailableBackends { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "Unable to identify current user";
                    return;
                }

                // Load chat history
                ChatHistory = await _context.AIChatHistories
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                // Calculate statistics
                TotalInteractions = await _context.AIChatHistories
                    .Where(h => h.UserId == userId)
                    .CountAsync();

                TotalCost = await _context.AIChatHistories
                    .Where(h => h.UserId == userId && h.EstimatedCost.HasValue)
                    .SumAsync(h => h.EstimatedCost ?? 0);

                TotalTokensUsed = await _context.AIChatHistories
                    .Where(h => h.UserId == userId && h.TokensUsed.HasValue)
                    .SumAsync(h => h.TokensUsed ?? 0);

                // Set available backends
                AvailableBackends = new List<string> { "Ollama", "Claude" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading AI Control Panel");
                ErrorMessage = "Error loading AI Control Panel: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(UserMessage))
                {
                    ErrorMessage = "Please enter a message";
                    return Page();
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    ErrorMessage = "Unable to identify current user";
                    return Page();
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Create AI request
                var request = new AIRequest
                {
                    Message = UserMessage,
                    RequestType = "chat"
                };

                // Process request through AI service
                var response = await _aiService.ProcessRequestAsync(request);
                stopwatch.Stop();

                if (response.Success)
                {
                    LastResponse = response.Content;
                    LastBackendUsed = response.BackendUsed;
                    LastEstimatedCost = response.EstimatedCost;
                    LastTokensUsed = response.TokensUsed;
                    ResponseTime = $"{stopwatch.ElapsedMilliseconds}ms";

                    // Save interaction to database
                    await _aiService.SaveInteractionAsync(
                        userId,
                        request,
                        response,
                        Enum.Parse<AIBackendType>(response.BackendUsed ?? "Ollama"));

                    _logger.LogInformation("AI request processed successfully. Backend: {Backend}, Cost: ${Cost}",
                        response.BackendUsed, response.EstimatedCost);
                }
                else
                {
                    ErrorMessage = response.ErrorMessage ?? "Unknown error occurred";
                    _logger.LogError("AI request failed: {Error}", response.ErrorMessage);
                }

                // Reload chat history
                await OnGetAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI request");
                ErrorMessage = "Error processing request: " + ex.Message;
                await OnGetAsync();
                return Page();
            }
        }
    }
}
