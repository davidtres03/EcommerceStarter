using System.Security.Claims;
using System.Text.Json;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class StripeKeysModel : PageModel
    {
        private const string STRIPE_API_TYPE = "Stripe";
        private const string STRIPE_LIVE_NAME = "Stripe-Live";
        private const string STRIPE_TEST_NAME = "Stripe-Test";

        private readonly ApplicationDbContext _context;
        private readonly IApiConfigurationService _apiConfigService;
        private readonly ILogger<StripeKeysModel> _logger;

        public StripeKeysModel(
            ApplicationDbContext context,
            IApiConfigurationService apiConfigService,
            ILogger<StripeKeysModel> logger)
        {
            _context = context;
            _apiConfigService = apiConfigService;
            _logger = logger;
        }

        public ApiConfiguration? CurrentConfiguration { get; set; }
        public List<ApiConfigurationAuditLog> RecentAudits { get; set; } = new();

        [BindProperty]
        public string PublishableKey { get; set; } = string.Empty;

        [BindProperty]
        public string SecretKey { get; set; } = string.Empty;

        [BindProperty]
        public string WebhookSecret { get; set; } = string.Empty;

        [BindProperty]
        public bool IsTestMode { get; set; } = true;

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get the active Stripe configuration (prefers Live, falls back to Test)
                CurrentConfiguration = await _context.ApiConfigurations
                    .Include(ac => ac.AuditLogs)
                    .FirstOrDefaultAsync(ac =>
                        ac.ApiType == STRIPE_API_TYPE &&
                        (ac.Name == STRIPE_LIVE_NAME || ac.Name == STRIPE_TEST_NAME) &&
                        ac.IsActive);

                if (CurrentConfiguration != null)
                {
                    IsTestMode = CurrentConfiguration.IsTestMode;
                    RecentAudits = CurrentConfiguration.AuditLogs
                        .OrderByDescending(a => a.Timestamp)
                        .Take(10)
                        .ToList();
                }

                // Log view action
                if (CurrentConfiguration != null)
                {
                    await _apiConfigService.MarkAsTestedAsync(
                        CurrentConfiguration.Id,
                        "SUCCESS",
                        "Configuration viewed",
                        User.FindFirstValue(ClaimTypes.NameIdentifier),
                        User.Identity?.Name
                    );
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Stripe configuration");
                ErrorMessage = "Error loading configuration";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Validate Stripe keys format
                if (!ValidateStripeKeys())
                {
                    await OnGetAsync(); // Reload data
                    return Page();
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.Identity?.Name;
                var configName = IsTestMode ? STRIPE_TEST_NAME : STRIPE_LIVE_NAME;

                // Prepare encrypted values
                var encryptedValues = new Dictionary<string, string?>
                {
                    { "EncryptedValue1", string.IsNullOrEmpty(PublishableKey) ? null : PublishableKey },
                    { "EncryptedValue2", string.IsNullOrEmpty(SecretKey) ? null : SecretKey },
                    { "EncryptedValue3", string.IsNullOrEmpty(WebhookSecret) ? null : WebhookSecret }
                };

                // Save configuration
                var config = await _apiConfigService.SaveConfigurationAsync(
                    apiType: STRIPE_API_TYPE,
                    name: configName,
                    encryptedValues: encryptedValues,
                    metadata: null,
                    description: $"Stripe {(IsTestMode ? "Test" : "Live")} Mode Configuration",
                    isTestMode: IsTestMode,
                    userId: userId,
                    userEmail: userEmail,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                _logger.LogWarning(
                    "Stripe API keys saved by {User}. Mode: {Mode}. ConfigId: {ConfigId}",
                    userEmail,
                    IsTestMode ? "Test" : "Live",
                    config.Id
                );

                SuccessMessage = "Stripe API keys saved successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Stripe configuration");
                ErrorMessage = $"Error saving configuration: {ex.Message}";
                await OnGetAsync(); // Reload data
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                var config = await _context.ApiConfigurations
                    .FirstOrDefaultAsync(ac =>
                        ac.ApiType == STRIPE_API_TYPE &&
                        (ac.Name == STRIPE_LIVE_NAME || ac.Name == STRIPE_TEST_NAME) &&
                        ac.IsActive);

                if (config != null)
                {
                    await _apiConfigService.DeleteConfigurationAsync(
                        config.Id,
                        User.FindFirstValue(ClaimTypes.NameIdentifier),
                        User.Identity?.Name
                    );

                    _logger.LogWarning("Stripe API configuration deleted by {User}", User.Identity?.Name);
                    SuccessMessage = "Stripe API keys deleted successfully.";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Stripe configuration");
                ErrorMessage = "Error deleting configuration";
                return RedirectToPage();
            }
        }

        private bool ValidateStripeKeys()
        {
            var hasErrors = false;

            if (!string.IsNullOrEmpty(PublishableKey))
            {
                var expectedPrefix = IsTestMode ? "pk_test_" : "pk_live_";
                if (!PublishableKey.StartsWith(expectedPrefix))
                {
                    ModelState.AddModelError(nameof(PublishableKey),
                        $"Publishable key must start with '{expectedPrefix}' for {(IsTestMode ? "test" : "live")} mode");
                    hasErrors = true;
                }
            }

            if (!string.IsNullOrEmpty(SecretKey))
            {
                var expectedPrefix = IsTestMode ? "sk_test_" : "sk_live_";
                if (!SecretKey.StartsWith(expectedPrefix))
                {
                    ModelState.AddModelError(nameof(SecretKey),
                        $"Secret key must start with '{expectedPrefix}' for {(IsTestMode ? "test" : "live")} mode");
                    hasErrors = true;
                }
            }

            if (!string.IsNullOrEmpty(WebhookSecret))
            {
                if (!WebhookSecret.StartsWith("whsec_"))
                {
                    ModelState.AddModelError(nameof(WebhookSecret),
                        "Webhook secret must start with 'whsec_'");
                    hasErrors = true;
                }
            }

            return !hasErrors;
        }
    }
}
