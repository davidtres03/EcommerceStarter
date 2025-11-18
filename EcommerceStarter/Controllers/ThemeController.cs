using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceStarter.Controllers
{
    /// <summary>
    /// API controller for serving dynamic theme CSS based on site settings
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ThemeController : ControllerBase
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<ThemeController> _logger;

        public ThemeController(
            ISiteSettingsService siteSettingsService,
            ILogger<ThemeController> logger)
        {
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the dynamic CSS based on current site settings
        /// Cache-busting: Use ?v=timestamp query parameter to force reload when settings change
        /// </summary>
        /// <returns>CSS content</returns>
        [HttpGet("css")]
        [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Client, VaryByQueryKeys = new[] { "v" })]
        public async Task<IActionResult> GetThemeCss([FromQuery] string? v = null)
        {
            try
            {
                var css = await _siteSettingsService.GenerateThemeCssAsync();
                
                // Add cache headers to help with proper caching
                Response.Headers.Append("Cache-Control", "public, max-age=1800");
                
                // If version parameter provided, add it to ETag for better cache validation
                if (!string.IsNullOrEmpty(v))
                {
                    Response.Headers.Append("ETag", $"\"{v}\"");
                }
                
                return Content(css, "text/css");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating theme CSS");
                // Return empty CSS on error to avoid breaking the site
                return Content("/* Error loading custom theme */", "text/css");
            }
        }
    }
}
