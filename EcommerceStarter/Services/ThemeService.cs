using EcommerceStarter.Models;
using System.Text.Json;

namespace EcommerceStarter.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<ThemeService> _logger;

        public ThemeService(
            ISiteSettingsService siteSettingsService,
            ILogger<ThemeService> logger)
        {
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        public async Task<string> ExportThemeAsync()
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                var theme = new
                {
                    SiteName = settings.SiteName,
                    SiteTagline = settings.SiteTagline,
                    SiteIcon = settings.SiteIcon,
                    PrimaryColor = settings.PrimaryColor,
                    PrimaryDark = settings.PrimaryDark,
                    PrimaryLight = settings.PrimaryLight,
                    SecondaryColor = settings.SecondaryColor,
                    AccentColor = settings.AccentColor,
                    PrimaryFont = settings.PrimaryFont,
                    HeadingFont = settings.HeadingFont,
                    ExportedDate = DateTime.UtcNow,
                    PlatformVersion = "1.0.0"
                };

                var json = JsonSerializer.Serialize(theme, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                _logger.LogInformation("Theme exported successfully");
                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting theme");
                throw;
            }
        }

        public async Task ImportThemeAsync(string json, string? importedBy = null)
        {
            try
            {
                var theme = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (theme == null)
                    throw new InvalidOperationException("Invalid theme JSON");

                var settings = await _siteSettingsService.GetSettingsAsync();

                // Import theme values
                if (theme.ContainsKey("SiteName"))
                    settings.SiteName = theme["SiteName"].GetString() ?? settings.SiteName;
                
                if (theme.ContainsKey("SiteTagline"))
                    settings.SiteTagline = theme["SiteTagline"].GetString() ?? settings.SiteTagline;
                
                if (theme.ContainsKey("SiteIcon"))
                    settings.SiteIcon = theme["SiteIcon"].GetString();
                
                if (theme.ContainsKey("PrimaryColor"))
                    settings.PrimaryColor = theme["PrimaryColor"].GetString() ?? settings.PrimaryColor;
                
                if (theme.ContainsKey("PrimaryDark"))
                    settings.PrimaryDark = theme["PrimaryDark"].GetString() ?? settings.PrimaryDark;
                
                if (theme.ContainsKey("PrimaryLight"))
                    settings.PrimaryLight = theme["PrimaryLight"].GetString() ?? settings.PrimaryLight;
                
                if (theme.ContainsKey("SecondaryColor"))
                    settings.SecondaryColor = theme["SecondaryColor"].GetString() ?? settings.SecondaryColor;
                
                if (theme.ContainsKey("AccentColor"))
                    settings.AccentColor = theme["AccentColor"].GetString() ?? settings.AccentColor;
                
                if (theme.ContainsKey("PrimaryFont"))
                    settings.PrimaryFont = theme["PrimaryFont"].GetString() ?? settings.PrimaryFont;
                
                if (theme.ContainsKey("HeadingFont"))
                    settings.HeadingFont = theme["HeadingFont"].GetString() ?? settings.HeadingFont;

                await _siteSettingsService.UpdateSettingsAsync(settings, importedBy);
                
                _logger.LogInformation("Theme imported successfully by {User}", importedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing theme");
                throw;
            }
        }

        public string GetPrebuiltTheme(string themeName)
        {
            var themes = GetThemeDefinitions();
            
            if (!themes.ContainsKey(themeName.ToLower()))
                throw new ArgumentException($"Theme '{themeName}' not found");

            var theme = themes[themeName.ToLower()];
            return JsonSerializer.Serialize(theme, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
        }

        public Dictionary<string, string> GetAvailableThemes()
        {
            return new Dictionary<string, string>
            {
                { "mushroom", "Mushroom - Earthy terracotta and sage (default)" },
                { "toys", "Playful Toys - Bright pink and turquoise" },
                { "electronics", "Modern Electronics - Clean blue and grey" },
                { "nature", "Natural/Organic - Fresh greens and browns" },
                { "bakery", "Cozy Bakery - Warm browns and cream" },
                { "fashion", "Elegant Fashion - Sophisticated black and gold" },
                { "sports", "Active Sports - Energetic red and navy" },
                { "books", "Literary Books - Classic burgundy and tan" }
            };
        }

        private Dictionary<string, object> GetThemeDefinitions()
        {
            return new Dictionary<string, object>
            {
                ["mushroom"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Modern E-Commerce Platform",
                    SiteIcon = "mushroom",
                    PrimaryColor = "#c77d3a",
                    PrimaryDark = "#a0642e",
                    PrimaryLight = "#d99960",
                    SecondaryColor = "#8b9a7a",
                    AccentColor = "#6b4423",
                    PrimaryFont = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif",
                    HeadingFont = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif"
                },
                ["toys"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Fun & Playful Shopping",
                    SiteIcon = "teddy bear",
                    PrimaryColor = "#FF6B9D",
                    PrimaryDark = "#C73866",
                    PrimaryLight = "#FFB3D9",
                    SecondaryColor = "#4ECDC4",
                    AccentColor = "#FFE66D",
                    PrimaryFont = "'Comic Sans MS', cursive",
                    HeadingFont = "'Comic Sans MS', cursive"
                },
                ["electronics"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Modern Technology Solutions",
                    SiteIcon = "laptop",
                    PrimaryColor = "#2196F3",
                    PrimaryDark = "#1976D2",
                    PrimaryLight = "#64B5F6",
                    SecondaryColor = "#607D8B",
                    AccentColor = "#00BCD4",
                    PrimaryFont = "Arial, Helvetica, sans-serif",
                    HeadingFont = "Arial, Helvetica, sans-serif"
                },
                ["nature"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Natural & Organic Products",
                    SiteIcon = "leaf",
                    PrimaryColor = "#8BC34A",
                    PrimaryDark = "#689F38",
                    PrimaryLight = "#AED581",
                    SecondaryColor = "#4CAF50",
                    AccentColor = "#795548",
                    PrimaryFont = "Georgia, serif",
                    HeadingFont = "Georgia, serif"
                },
                ["bakery"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Fresh Baked Goods Daily",
                    SiteIcon = "bread",
                    PrimaryColor = "#D2691E",
                    PrimaryDark = "#A0522D",
                    PrimaryLight = "#F4A460",
                    SecondaryColor = "#FFE4B5",
                    AccentColor = "#8B4513",
                    PrimaryFont = "Georgia, serif",
                    HeadingFont = "Georgia, serif"
                },
                ["fashion"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Elegant Style & Fashion",
                    SiteIcon = "dress",
                    PrimaryColor = "#212121",
                    PrimaryDark = "#000000",
                    PrimaryLight = "#424242",
                    SecondaryColor = "#FFD700",
                    AccentColor = "#C0C0C0",
                    PrimaryFont = "Georgia, serif",
                    HeadingFont = "Georgia, serif"
                },
                ["sports"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Gear Up For Victory",
                    SiteIcon = "sports equipment",
                    PrimaryColor = "#D32F2F",
                    PrimaryDark = "#B71C1C",
                    PrimaryLight = "#EF5350",
                    SecondaryColor = "#1976D2",
                    AccentColor = "#FFC107",
                    PrimaryFont = "Impact, Charcoal, sans-serif",
                    HeadingFont = "Impact, Charcoal, sans-serif"
                },
                ["books"] = new
                {
                    SiteName = "My Store",
                    SiteTagline = "Discover Great Reads",
                    SiteIcon = "book",
                    PrimaryColor = "#8B0000",
                    PrimaryDark = "#5C0000",
                    PrimaryLight = "#A52A2A",
                    SecondaryColor = "#D2B48C",
                    AccentColor = "#654321",
                    PrimaryFont = "'Times New Roman', Times, serif",
                    HeadingFont = "'Times New Roman', Times, serif"
                }
            };
        }
    }
}
