using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class ApiTestingModel : PageModel
    {
        private readonly ISiteSettingsService _settingsService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<ApiTestingModel> _logger;

        public ApiTestingModel(
            ISiteSettingsService settingsService,
            IEncryptionService encryptionService,
            ILogger<ApiTestingModel> logger)
        {
            _settingsService = settingsService;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public string ServiceKey { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Internal Service Auth feature has been disabled
            // API keys now managed via ApiConfigurations table
            IsEnabled = false;
            ServiceKey = "[Feature Disabled - Use ApiConfigurations]";

            return Page();
        }

        public async Task<IActionResult> OnPostToggleAuthAsync(bool enable)
        {
            // Feature disabled
            return RedirectToPage("/Admin/Settings/ApiTesting");
        }

        public async Task<IActionResult> OnPostRegenerateKeyAsync()
        {
            // Feature disabled
            TempData["ErrorMessage"] = "Internal Service Key feature has been disabled. Use ApiConfigurations instead.";
            return RedirectToPage("/Admin/Settings/ApiTesting");
        }
    }
}
