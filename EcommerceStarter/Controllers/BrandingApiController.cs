using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace EcommerceStarter.Controllers
{
    [ApiController]
    [Route("api/branding")]
    public class BrandingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrandingController> _logger;

        public BrandingController(
            IConfiguration configuration,
            ILogger<BrandingController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetBranding()
        {
            try
            {
                // Read branding configuration from appsettings.json or database
                var branding = new BrandingResponse
                {
                    BusinessName = _configuration["Branding:BusinessName"] ?? "EcommerceStarter",
                    LogoUrl = _configuration["Branding:LogoUrl"] ?? "",
                    PrimaryColor = _configuration["Branding:PrimaryColor"] ?? "#6200EE",
                    SecondaryColor = _configuration["Branding:SecondaryColor"] ?? "#03DAC6",
                    AccentColor = _configuration["Branding:AccentColor"] ?? "#FF6B6B",
                    BackgroundColor = _configuration["Branding:BackgroundColor"] ?? "#FFFFFF",
                    SurfaceColor = _configuration["Branding:SurfaceColor"] ?? "#F5F5F5",
                    TextPrimaryColor = _configuration["Branding:TextPrimaryColor"] ?? "#000000",
                    TextSecondaryColor = _configuration["Branding:TextSecondaryColor"] ?? "#757575",
                    FaviconUrl = _configuration["Branding:FaviconUrl"] ?? "",
                    SupportEmail = _configuration["Branding:SupportEmail"] ?? "",
                    SupportPhone = _configuration["Branding:SupportPhone"] ?? ""
                };

                return Ok(branding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving branding configuration");
                
                // Return default branding on error
                return Ok(new BrandingResponse
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
                });
            }
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
