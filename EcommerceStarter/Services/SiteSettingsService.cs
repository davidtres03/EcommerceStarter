using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Implementation of site settings service with in-memory caching
    /// </summary>
    public class SiteSettingsService : ISiteSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SiteSettingsService> _logger;
        private const string CACHE_KEY = "SiteSettings";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        // Static sanitizer instance reused for performance
        private static readonly HtmlSanitizer _sanitizer = CreateSanitizer();

        public SiteSettingsService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<SiteSettingsService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        private static HtmlSanitizer CreateSanitizer()
        {
            var sanitizer = new HtmlSanitizer();

            // Configure allowed tags conservatively
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.Add("a");
            sanitizer.AllowedTags.Add("b");
            sanitizer.AllowedTags.Add("i");
            sanitizer.AllowedTags.Add("strong");
            sanitizer.AllowedTags.Add("em");
            sanitizer.AllowedTags.Add("p");
            sanitizer.AllowedTags.Add("ul");
            sanitizer.AllowedTags.Add("ol");
            sanitizer.AllowedTags.Add("li");
            sanitizer.AllowedTags.Add("br");
            sanitizer.AllowedTags.Add("span");
            sanitizer.AllowedTags.Add("div");
            sanitizer.AllowedTags.Add("img");

            // Configure allowed attributes
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("href");
            sanitizer.AllowedAttributes.Add("src");
            sanitizer.AllowedAttributes.Add("alt");
            sanitizer.AllowedAttributes.Add("title");
            sanitizer.AllowedAttributes.Add("class");

            // Allow only http(s) href/src
            sanitizer.UriAttributes.Add("href");
            sanitizer.UriAttributes.Add("src");
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.Add("http");
            sanitizer.AllowedSchemes.Add("https");

            // Disallow iframes, scripts, forms, and event attributes by default
            sanitizer.RemovingAttribute += (s, e) => {
                // No-op; kept for potential logging
            };

            return sanitizer;
        }

        public async Task<SiteSettings> GetSettingsAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(CACHE_KEY, out SiteSettings? cachedSettings) && cachedSettings != null)
            {
                return cachedSettings;
            }

            // Not in cache, load from database
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();

            // If no settings exist, create default settings
            if (settings == null)
            {
                _logger.LogInformation("No site settings found, creating defaults");
                settings = CreateDefaultSettings();
                _context.SiteSettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            // Cache the settings
            _cache.Set(CACHE_KEY, settings, _cacheDuration);

            return settings;
        }

        public async Task UpdateSettingsAsync(SiteSettings settings, string? modifiedBy = null)
        {
            try
            {
                var existingSettings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (existingSettings == null)
                {
                    // No settings exist, add new
                    settings.LastModified = DateTime.UtcNow;
                    settings.LastModifiedBy = modifiedBy;

                    // Sanitize any custom HTML before saving
                    settings.CustomHeaderHtml = SanitizeHtml(settings.CustomHeaderHtml);
                    settings.CustomFooterHtml = SanitizeHtml(settings.CustomFooterHtml);

                    _context.SiteSettings.Add(settings);
                }
                else
                {
                    // Update existing settings
                    existingSettings.SiteName = settings.SiteName;
                    existingSettings.SiteTagline = settings.SiteTagline;
                    existingSettings.LogoUrl = settings.LogoUrl;
                    existingSettings.LogoImageId = settings.LogoImageId;
                    existingSettings.HorizontalLogoUrl = settings.HorizontalLogoUrl;
                    existingSettings.HorizontalLogoImageId = settings.HorizontalLogoImageId;
                    existingSettings.FaviconUrl = settings.FaviconUrl;
                    existingSettings.FaviconImageId = settings.FaviconImageId;
                    existingSettings.HeroImageUrl = settings.HeroImageUrl;
                    existingSettings.HeroImageId = settings.HeroImageId;
                    existingSettings.SiteIcon = settings.SiteIcon;

                    // Colors
                    existingSettings.PrimaryColor = settings.PrimaryColor;
                    existingSettings.PrimaryDark = settings.PrimaryDark;
                    existingSettings.PrimaryLight = settings.PrimaryLight;
                    existingSettings.SecondaryColor = settings.SecondaryColor;
                    existingSettings.AccentColor = settings.AccentColor;

                    // Typography
                    existingSettings.PrimaryFont = settings.PrimaryFont;
                    existingSettings.HeadingFont = settings.HeadingFont;

                    // Business Info
                    existingSettings.CompanyName = settings.CompanyName;
                    existingSettings.ContactEmail = settings.ContactEmail;
                    existingSettings.SupportEmail = settings.SupportEmail;
                    existingSettings.Phone = settings.Phone;
                    existingSettings.Address = settings.Address;
                    existingSettings.City = settings.City;
                    existingSettings.State = settings.State;
                    existingSettings.PostalCode = settings.PostalCode;
                    existingSettings.Country = settings.Country;

                    // Social Media
                    existingSettings.FacebookUrl = settings.FacebookUrl;
                    existingSettings.TwitterUrl = settings.TwitterUrl;
                    existingSettings.InstagramUrl = settings.InstagramUrl;
                    existingSettings.LinkedInUrl = settings.LinkedInUrl;
                    existingSettings.YouTubeUrl = settings.YouTubeUrl;

                    // SEO
                    existingSettings.MetaDescription = settings.MetaDescription;
                    existingSettings.MetaKeywords = settings.MetaKeywords;

                    // Features
                    existingSettings.EnableGuestCheckout = settings.EnableGuestCheckout;
                    existingSettings.EnableProductReviews = settings.EnableProductReviews;
                    existingSettings.EnableWishlist = settings.EnableWishlist;
                    existingSettings.ShowStockCount = settings.ShowStockCount;
                    existingSettings.AllowBackorders = settings.AllowBackorders;

                    // Email
                    existingSettings.EnableEmailNotifications = settings.EnableEmailNotifications;
                    existingSettings.EmailProvider = settings.EmailProvider;
                    existingSettings.ApiConfigurationId = settings.ApiConfigurationId;

                    existingSettings.SmtpHost = settings.SmtpHost;
                    existingSettings.SmtpPort = settings.SmtpPort;
                    existingSettings.SmtpUsername = settings.SmtpUsername;
                    existingSettings.SmtpPassword = settings.SmtpPassword;
                    existingSettings.SmtpUseSsl = settings.SmtpUseSsl;

                    existingSettings.SendOrderConfirmationEmails = settings.SendOrderConfirmationEmails;
                    existingSettings.SendShippingNotificationEmails = settings.SendShippingNotificationEmails;
                    existingSettings.SendAdminOrderNotifications = settings.SendAdminOrderNotifications;
                    existingSettings.AdminNotificationEmail = settings.AdminNotificationEmail;

                    existingSettings.EmailLogoUrl = settings.EmailLogoUrl;
                    existingSettings.EmailLogoImageId = settings.EmailLogoImageId;
                    existingSettings.EmailHeaderColor = settings.EmailHeaderColor;
                    existingSettings.EmailButtonColor = settings.EmailButtonColor;
                    existingSettings.EmailFooterText = settings.EmailFooterText;

                    // Common email addresses
                    existingSettings.EmailFromName = settings.EmailFromName;
                    existingSettings.EmailFromAddress = settings.EmailFromAddress;
                    existingSettings.EmailSupportAddress = settings.EmailSupportAddress;

                    // Custom CSS/HTML - sanitize custom header/footer HTML
                    existingSettings.CustomCss = settings.CustomCss;
                    existingSettings.CustomHeaderHtml = SanitizeHtml(settings.CustomHeaderHtml);
                    existingSettings.CustomFooterHtml = SanitizeHtml(settings.CustomFooterHtml);

                    // Google Analytics (Cloudflare Gateway)
                    existingSettings.GoogleAnalyticsMeasurementId = settings.GoogleAnalyticsMeasurementId;
                    existingSettings.MeasurementPath = settings.MeasurementPath ?? "/metrics";

                    // Metadata
                    existingSettings.LastModified = DateTime.UtcNow;
                    existingSettings.LastModifiedBy = modifiedBy;
                }

                await _context.SaveChangesAsync();

                // Clear cache to force reload
                ClearCache();

                _logger.LogInformation("Site settings updated by {ModifiedBy}", modifiedBy ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site settings");
                throw;
            }
        }

        public async Task<string> GenerateThemeCssAsync()
        {
            var settings = await GetSettingsAsync();
            return settings.GenerateThemeCss();
        }

        public async Task ResetToDefaultsAsync(string? modifiedBy = null)
        {
            var defaultSettings = CreateDefaultSettings();
            defaultSettings.LastModifiedBy = modifiedBy;
            await UpdateSettingsAsync(defaultSettings, modifiedBy);

            _logger.LogInformation("Site settings reset to defaults by {ModifiedBy}", modifiedBy ?? "Unknown");
        }

        public void ClearCache()
        {
            _cache.Remove(CACHE_KEY);
            _logger.LogInformation("? Site settings cache cleared - fresh settings will load on next request");
        }

        /// <summary>
        /// Basic HTML sanitizer to remove potentially dangerous tags and attributes.
        /// This is conservative: it removes <script>, <style>, <iframe>, <object>, <embed>, and form-related tags,
        /// strips event handler attributes (on*), and removes javascript: URIs.
        /// For production use, prefer a robust library like Ganss.XSS.HtmlSanitizer.
        /// </summary>
        private string? SanitizeHtml(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            try
            {
                return input == null ? null : _sanitizer.Sanitize(input);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SanitizeHtml failed; returning empty string to be safe");
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates default settings (current mushroom theme)
        /// </summary>
        private SiteSettings CreateDefaultSettings()
        {
            return new SiteSettings
            {
                // Branding
                SiteName = "My Store",
                SiteTagline = "Modern E-Commerce Platform Built with ASP.NET Core",
                SiteIcon = "??",  // Shopping cart emoji - user can change
                
                // Colors (Professional blue theme)
                PrimaryColor = "#0d6efd",
                PrimaryDark = "#0a58ca",
                PrimaryLight = "#6ea8fe",
                SecondaryColor = "#6c757d",
                AccentColor = "#0dcaf0",
                
                // Typography
                PrimaryFont = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif",
                HeadingFont = "Segoe UI, Tahoma, Geneva, Verdana, sans-serif",
                
                // Business Info
                CompanyName = "My Store",
                ContactEmail = "contact@example.com",
                SupportEmail = "support@example.com",
                
                // SEO
                MetaDescription = "Modern e-commerce platform built with ASP.NET Core. Customize and launch your online store today.",
                MetaKeywords = "e-commerce, online store, asp.net core, shopping",
                
                // Features
                EnableGuestCheckout = true,
                EnableProductReviews = false,
                EnableWishlist = false,
                ShowStockCount = true,
                AllowBackorders = false,
                
                // Email
                EmailFromName = "My Store",
                EmailFromAddress = "noreply@example.com",
                
                // Metadata
                LastModified = DateTime.UtcNow,
                LastModifiedBy = "System"
            };
        }
    }
}
