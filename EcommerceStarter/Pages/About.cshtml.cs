using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages
{
    public class AboutModel : PageModel
    {
        private readonly ISiteSettingsService _siteSettingsService;

        public AboutModel(ISiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        public string BusinessName { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            BusinessName = settings.SiteName;
            SupportEmail = settings.SupportEmail;
        }
    }
}
