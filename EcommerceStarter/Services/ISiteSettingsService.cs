using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for managing site-wide settings and branding
    /// </summary>
    public interface ISiteSettingsService
    {
        /// <summary>
        /// Gets the current site settings (cached)
        /// </summary>
        Task<SiteSettings> GetSettingsAsync();

        /// <summary>
        /// Updates site settings
        /// </summary>
        Task UpdateSettingsAsync(SiteSettings settings, string? modifiedBy = null);

        /// <summary>
        /// Generates dynamic CSS from current theme settings
        /// </summary>
        Task<string> GenerateThemeCssAsync();

        /// <summary>
        /// Resets settings to default values
        /// </summary>
        Task ResetToDefaultsAsync(string? modifiedBy = null);

        /// <summary>
        /// Clears the settings cache (forces reload from database)
        /// </summary>
        void ClearCache();
    }
}
