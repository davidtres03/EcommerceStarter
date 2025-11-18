using System.Text.Json;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for exporting and importing theme configurations
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Exports current theme to JSON
        /// </summary>
        Task<string> ExportThemeAsync();

        /// <summary>
        /// Imports theme from JSON
        /// </summary>
        Task ImportThemeAsync(string json, string? importedBy = null);

        /// <summary>
        /// Gets pre-built theme by name
        /// </summary>
        string GetPrebuiltTheme(string themeName);

        /// <summary>
        /// Lists available pre-built themes
        /// </summary>
        Dictionary<string, string> GetAvailableThemes();
    }
}
