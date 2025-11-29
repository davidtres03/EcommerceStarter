using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceStarter.ViewComponents
{
    /// <summary>
    /// View component that provides site settings to all views
    /// Automatically injects settings without requiring manual injection
    /// </summary>
    public class SiteSettingsViewComponent : ViewComponent
    {
        private readonly ISiteSettingsService _siteSettingsService;

        public SiteSettingsViewComponent(ISiteSettingsService siteSettingsService)
        {
            _siteSettingsService = siteSettingsService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            return View(settings);
        }
    }
}
