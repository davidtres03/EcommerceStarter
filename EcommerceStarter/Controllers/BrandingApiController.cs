using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Services;

namespace EcommerceStarter.Controllers
{
    [ApiController]
    [Route("api/branding")]
    public class BrandingController : ControllerBase
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<BrandingController> _logger;

        public BrandingController(
            ISiteSettingsService siteSettingsService,
            ILogger<BrandingController> logger)
        {
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetBranding()
        {
            try
            {
                _logger.LogInformation("Fetching branding configuration from database");
                
                // Read branding configuration from database via SiteSettingsService
                var siteSettings = await _siteSettingsService.GetSettingsAsync();

                if (siteSettings == null)
                {
                    _logger.LogWarning("Site settings returned null from service");
                    return Ok(GetDefaultBranding());
                }

                _logger.LogInformation("Retrieved site settings successfully: {Name}, Colors: {Primary}/{Secondary}/{Accent}", 
                    siteSettings.CompanyName, siteSettings.PrimaryColor, siteSettings.SecondaryColor, siteSettings.AccentColor);

                var branding = new BrandingResponse
                {
                    BusinessName = siteSettings.CompanyName ?? siteSettings.SiteName,
                    LogoUrl = !string.IsNullOrEmpty(siteSettings.LogoUrl) 
                        ? $"https://capandcollarsupplyco.com{siteSettings.LogoUrl}" 
                        : "",
                    PrimaryColor = siteSettings.PrimaryColor,
                    SecondaryColor = siteSettings.SecondaryColor,
                    AccentColor = siteSettings.AccentColor,
                    BackgroundColor = "#FFFFFF",
                    SurfaceColor = "#F8F9FA",
                    TextPrimaryColor = "#212529",
                    TextSecondaryColor = "#6C757D",
                    FaviconUrl = !string.IsNullOrEmpty(siteSettings.FaviconUrl)
                        ? $"https://capandcollarsupplyco.com{siteSettings.FaviconUrl}"
                        : "",
                    SupportEmail = siteSettings.SupportEmail,
                    SupportPhone = siteSettings.Phone ?? ""
                };

                return Ok(branding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branding configuration from database");
                return Ok(GetDefaultBranding());
            }
        }

        private BrandingResponse GetDefaultBranding()
        {
            return new BrandingResponse
            {
                BusinessName = "EcommerceStarter",
                LogoUrl = "",
                PrimaryColor = "#6200EE",
                SecondaryColor = "#03DAC6",
                AccentColor = "#FF6B6B",
                BackgroundColor = "#FFFFFF",
                SurfaceColor = "#F5F5F5",
                TextPrimaryColor = "#000000",
                TextSecondaryColor = "#757575",
                FaviconUrl = "",
                SupportEmail = "",
                SupportPhone = ""
            };
        }
    }

    public class BrandingResponse
    {
        public string BusinessName { get; set; } = "";
        public string LogoUrl { get; set; } = "";
        public string PrimaryColor { get; set; } = "";
        public string SecondaryColor { get; set; } = "";
        public string AccentColor { get; set; } = "";
        public string BackgroundColor { get; set; } = "";
        public string SurfaceColor { get; set; } = "";
        public string TextPrimaryColor { get; set; } = "";
        public string TextSecondaryColor { get; set; } = "";
        public string FaviconUrl { get; set; } = "";
        public string SupportEmail { get; set; } = "";
        public string SupportPhone { get; set; } = "";
    }
}
