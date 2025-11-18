using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Policy = "AdminOnly")]
    public class SecurityModel : PageModel
    {
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly ILogger<SecurityModel> _logger;

        public SecurityModel(ISecuritySettingsService securitySettingsService, ILogger<SecurityModel> logger)
        {
            _securitySettingsService = securitySettingsService;
            _logger = logger;
        }

        [BindProperty]
        public SecuritySettings Settings { get; set; } = new();

        public async Task OnGetAsync()
        {
            Settings = await _securitySettingsService.GetSettingsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userEmail = User.Identity?.Name ?? "Unknown Admin";
                await _securitySettingsService.UpdateSettingsAsync(Settings, userEmail);

                TempData["SuccessMessage"] = "Security settings updated successfully!";
                _logger.LogInformation("Security settings updated by {User}", userEmail);
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating security settings");
                TempData["ErrorMessage"] = "Failed to update security settings. Please try again.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResetToDefaultsAsync()
        {
            try
            {
                var defaultSettings = new SecuritySettings();
                var userEmail = User.Identity?.Name ?? "Unknown Admin";
                await _securitySettingsService.UpdateSettingsAsync(defaultSettings, userEmail);

                TempData["SuccessMessage"] = "Security settings reset to defaults successfully!";
                _logger.LogInformation("Security settings reset to defaults by {User}", userEmail);
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting security settings");
                TempData["ErrorMessage"] = "Failed to reset security settings. Please try again.";
                return RedirectToPage();
            }
        }
    }
}
