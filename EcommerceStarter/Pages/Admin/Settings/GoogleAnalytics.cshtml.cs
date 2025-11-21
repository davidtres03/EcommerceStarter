using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class GoogleAnalyticsModel : PageModel
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<GoogleAnalyticsModel> _logger;

        public GoogleAnalyticsModel(
            ISiteSettingsService siteSettingsService,
            ILogger<GoogleAnalyticsModel> logger)
        {
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        [BindProperty]
        public string? GoogleTagManagerId { get; set; }

        [BindProperty]
        public string? MeasurementPath { get; set; }

        public async Task OnGetAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            GoogleTagManagerId = settings.GoogleAnalyticsMeasurementId;
            MeasurementPath = string.IsNullOrEmpty(settings.MeasurementPath) ? "/metrics" : settings.MeasurementPath;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Sanitize the measurement path to remove any invalid characters
                if (!string.IsNullOrEmpty(MeasurementPath))
                {
                    // Remove any whitespace, newlines, carriage returns, and control characters
                    MeasurementPath = MeasurementPath.Trim();
                    MeasurementPath = new string(MeasurementPath.Where(c => c >= 32 && c <= 126 && c != '\r' && c != '\n').ToArray());
                    
                    // Validate that it starts with /
                    if (!MeasurementPath.StartsWith("/"))
                    {
                        ModelState.AddModelError(nameof(MeasurementPath), "Measurement path must start with /");
                        return Page();
                    }
                    
                    // Validate it only contains allowed characters
                    if (!System.Text.RegularExpressions.Regex.IsMatch(MeasurementPath, @"^/[a-zA-Z0-9/]+$"))
                    {
                        ModelState.AddModelError(nameof(MeasurementPath), "Measurement path can only contain letters, numbers, and forward slashes");
                        return Page();
                    }
                }
                
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                // Store GTM ID and measurement path
                settings.GoogleAnalyticsMeasurementId = GoogleTagManagerId?.Trim();
                settings.MeasurementPath = MeasurementPath?.Trim();
                
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                TempData["Success"] = "Google Tag settings saved successfully!";
                _logger.LogInformation("Google Tag settings updated by {User}: Tag ID={TagId}, Path={Path}", 
                    User.Identity?.Name, GoogleTagManagerId, MeasurementPath);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Google Tag settings");
                TempData["Error"] = $"Failed to save settings: {ex.Message}";
                return Page();
            }
        }
    }
}
