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
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<StripeKeysModel> _logger;

        public StripeKeysModel(
            ApplicationDbContext context,
            IEncryptionService encryption,
            ILogger<StripeKeysModel> logger)
        {
            _context = context;
            _encryption = encryption;
            _logger = logger;
        }

        public StripeConfiguration? CurrentConfiguration { get; set; }
        public List<StripeConfigurationAuditLog> RecentAudits { get; set; } = new();

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
                CurrentConfiguration = await _context.StripeConfigurations.FirstOrDefaultAsync();

                if (CurrentConfiguration != null)
                {
                    IsTestMode = CurrentConfiguration.IsTestMode;
                }

                // Load recent audit logs
                RecentAudits = await _context.StripeConfigurationAuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync();

                // Log view action
                await LogAuditAsync("Viewed", null);

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

                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();
                var isNewConfig = config == null;

                if (config == null)
                {
                    config = new StripeConfiguration();
                    _context.StripeConfigurations.Add(config);
                }

                // Track changes for audit
                var changes = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(PublishableKey))
                {
                    config.EncryptedPublishableKey = _encryption.Encrypt(PublishableKey);
                    changes["PublishableKey"] = "Updated";
                }

                if (!string.IsNullOrEmpty(SecretKey))
                {
                    config.EncryptedSecretKey = _encryption.Encrypt(SecretKey);
                    changes["SecretKey"] = "Updated";
                }

                if (!string.IsNullOrEmpty(WebhookSecret))
                {
                    config.EncryptedWebhookSecret = _encryption.Encrypt(WebhookSecret);
                    changes["WebhookSecret"] = "Updated";
                }

                config.IsTestMode = IsTestMode;
                config.LastUpdated = DateTime.UtcNow;
                config.UpdatedBy = User.Identity?.Name;

                changes["Mode"] = IsTestMode ? "Test Mode" : "Live Mode";

                await _context.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(isNewConfig ? "Created" : "Updated", JsonSerializer.Serialize(changes));

                _logger.LogWarning(
                    "Stripe API keys {Action} by {User}. Mode: {Mode}",
                    isNewConfig ? "created" : "updated",
                    User.Identity?.Name,
                    IsTestMode ? "Test" : "Live"
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
                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();

                if (config != null)
                {
                    _context.StripeConfigurations.Remove(config);
                    await _context.SaveChangesAsync();

                    await LogAuditAsync("Deleted", "All keys removed");

                    _logger.LogWarning("Stripe API keys deleted by {User}", User.Identity?.Name);

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

        private async Task LogAuditAsync(string action, string? changes)
        {
            try
            {
                var audit = new StripeConfigurationAuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    UserEmail = User.Identity?.Name,
                    Action = action,
                    Changes = changes,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    WasTestMode = IsTestMode
                };

                _context.StripeConfigurationAuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging audit");
                // Don't throw - audit failure shouldn't block the main action
            }
        }
    }
}
