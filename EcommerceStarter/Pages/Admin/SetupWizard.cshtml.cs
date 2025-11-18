using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SetupWizardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IThemeService _themeService;
        private readonly ILogger<SetupWizardModel> _logger;

        public SetupWizardModel(
            ApplicationDbContext context,
            ISiteSettingsService siteSettingsService,
            IThemeService themeService,
            ILogger<SetupWizardModel> logger)
        {
            _context = context;
            _siteSettingsService = siteSettingsService;
            _themeService = themeService;
            _logger = logger;
        }

        [BindProperty]
        public string StoreName { get; set; } = string.Empty;

        [BindProperty]
        public string StoreTagline { get; set; } = string.Empty;

        [BindProperty]
        public string StoreIcon { get; set; } = string.Empty;

        [BindProperty]
        public string SelectedTheme { get; set; } = "mushroom";

        [BindProperty]
        public string ContactEmail { get; set; } = string.Empty;

        public int CurrentStep { get; set; } = 1;
        public Dictionary<string, string> AvailableThemes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int step = 1)
        {
            // Check if setup is already complete
            var setupStatus = await _context.SetupStatus.FirstOrDefaultAsync();
            if (setupStatus?.IsSetupComplete == true)
            {
                return RedirectToPage("/Admin/Dashboard");
            }

            CurrentStep = step;
            AvailableThemes = _themeService.GetAvailableThemes();

            // Load current settings for pre-fill
            if (step > 1)
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                StoreName = settings.SiteName;
                StoreTagline = settings.SiteTagline;
                StoreIcon = settings.SiteIcon ?? string.Empty;
                ContactEmail = settings.ContactEmail;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStep1Async()
        {
            if (string.IsNullOrWhiteSpace(StoreName))
            {
                ModelState.AddModelError(nameof(StoreName), "Store name is required");
                CurrentStep = 1;
                AvailableThemes = _themeService.GetAvailableThemes();
                return Page();
            }

            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.SiteName = StoreName;
                settings.SiteTagline = StoreTagline;
                settings.SiteIcon = StoreIcon;
                settings.CompanyName = StoreName;

                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                return RedirectToPage(new { step = 2 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in setup step 1");
                ModelState.AddModelError("", "An error occurred. Please try again.");
                CurrentStep = 1;
                AvailableThemes = _themeService.GetAvailableThemes();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostStep2Async()
        {
            try
            {
                // Apply selected theme
                var themeJson = _themeService.GetPrebuiltTheme(SelectedTheme);
                await _themeService.ImportThemeAsync(themeJson, User.Identity?.Name);

                // Update with user's store name
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.SiteName = StoreName;
                settings.SiteTagline = StoreTagline;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                return RedirectToPage(new { step = 3 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in setup step 2");
                ModelState.AddModelError("", "An error occurred applying the theme.");
                CurrentStep = 2;
                AvailableThemes = _themeService.GetAvailableThemes();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostStep3Async()
        {
            try
            {
                // Update contact information
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.ContactEmail = ContactEmail;
                settings.SupportEmail = ContactEmail;
                settings.EmailFromAddress = ContactEmail;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                return RedirectToPage(new { step = 4 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in setup step 3");
                ModelState.AddModelError("", "An error occurred. Please try again.");
                CurrentStep = 3;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCompleteAsync()
        {
            try
            {
                // Mark setup as complete
                var setupStatus = await _context.SetupStatus.FirstOrDefaultAsync();
                if (setupStatus == null)
                {
                    setupStatus = new SetupStatus();
                    _context.SetupStatus.Add(setupStatus);
                }

                setupStatus.IsSetupComplete = true;
                setupStatus.SetupCompletedDate = DateTime.UtcNow;
                setupStatus.SetupCompletedBy = User.Identity?.Name;
                setupStatus.HasConfiguredBranding = true;
                setupStatus.InitialTheme = SelectedTheme;
                setupStatus.PlatformVersion = "1.0.0";
                setupStatus.LastModified = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Setup completed successfully! Welcome to your new store.";
                _logger.LogInformation("Setup wizard completed by {User}", User.Identity?.Name);

                return RedirectToPage("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing setup");
                TempData["ErrorMessage"] = "An error occurred completing setup.";
                return RedirectToPage(new { step = 4 });
            }
        }
    }
}
