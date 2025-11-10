using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Site-wide branding and configuration settings
    /// Allows customization of theme, colors, logos, and business information
    /// All values can be configured through the Admin Panel or Setup Wizard
    /// </summary>
    public class SiteSettings
    {
        public int Id { get; set; }

        // ============================================================
        // Branding
        // ============================================================

        [Required]
        [StringLength(100)]
        [Display(Name = "Site Name")]
        public string SiteName { get; set; } = "My Store";

        [StringLength(200)]
        [Display(Name = "Site Tagline")]
        public string SiteTagline { get; set; } = "Powered by EcommerceStarter";

        [StringLength(500)]
        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; } = "/images/logo.png";

        [StringLength(500)]
        [Display(Name = "Favicon URL")]
        public string? FaviconUrl { get; set; } = "/favicon.ico";

        [StringLength(500)]
        [Display(Name = "Hero Image URL")]
        public string? HeroImageUrl { get; set; } = "/images/hero-bg.jpg";

        [StringLength(50)]
        [Display(Name = "Site Icon/Emoji")]
        public string? SiteIcon { get; set; } = "??";  // Shopping cart emoji - generic e-commerce

        // ============================================================
        // Theme Colors (Bootstrap 5 Defaults - Neutral Blue Theme)
        // ============================================================

        [Required]
        [StringLength(20)]
        [Display(Name = "Primary Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format (e.g., #0d6efd)")]
        public string PrimaryColor { get; set; } = "#0d6efd";  // Bootstrap primary blue

        [Required]
        [StringLength(20)]
        [Display(Name = "Primary Dark")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string PrimaryDark { get; set; } = "#0a58ca";  // Bootstrap primary dark

        [Required]
        [StringLength(20)]
        [Display(Name = "Primary Light")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string PrimaryLight { get; set; } = "#6ea8fe";  // Bootstrap primary light

        [Required]
        [StringLength(20)]
        [Display(Name = "Secondary Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string SecondaryColor { get; set; } = "#6c757d";  // Bootstrap secondary gray

        [Required]
        [StringLength(20)]
        [Display(Name = "Accent Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string AccentColor { get; set; } = "#198754";  // Bootstrap success green

        // ============================================================
        // Typography
        // ============================================================

        [StringLength(100)]
        [Display(Name = "Primary Font")]
        public string PrimaryFont { get; set; } = "system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

        [StringLength(100)]
        [Display(Name = "Heading Font")]
        public string HeadingFont { get; set; } = "system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";

        // ============================================================
        // Business Information (Configure through Setup Wizard)
        // ============================================================

        [Required]
        [StringLength(200)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = "My Store";

        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = "contact@example.com";

        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Support Email")]
        public string SupportEmail { get; set; } = "support@example.com";

        [Phone]
        [StringLength(50)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100)]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(50)]
        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [StringLength(100)]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        // ============================================================
        // Social Media Links
        // ============================================================

        [Url]
        [StringLength(500)]
        [Display(Name = "Facebook URL")]
        public string? FacebookUrl { get; set; }

        [Url]
        [StringLength(500)]
        [Display(Name = "Twitter/X URL")]
        public string? TwitterUrl { get; set; }

        [Url]
        [StringLength(500)]
        [Display(Name = "Instagram URL")]
        public string? InstagramUrl { get; set; }

        [Url]
        [StringLength(500)]
        [Display(Name = "LinkedIn URL")]
        public string? LinkedInUrl { get; set; }

        [Url]
        [StringLength(500)]
        [Display(Name = "YouTube URL")]
        public string? YouTubeUrl { get; set; }

        // ============================================================
        // SEO Settings
        // ============================================================

        [StringLength(500)]
        [Display(Name = "Meta Description")]
        public string MetaDescription { get; set; } = "Modern e-commerce platform for your business. Sell products, manage orders, and grow your online store.";

        [StringLength(500)]
        [Display(Name = "Meta Keywords")]
        public string? MetaKeywords { get; set; } = "ecommerce, online store, sales, products, shopping";

        // ============================================================
        // Feature Toggles
        // ============================================================

        [Display(Name = "Enable Guest Checkout")]
        public bool EnableGuestCheckout { get; set; } = true;

        [Display(Name = "Enable Product Reviews")]
        public bool EnableProductReviews { get; set; } = false;

        [Display(Name = "Enable Wishlist")]
        public bool EnableWishlist { get; set; } = false;

        [Display(Name = "Show Product Stock Count")]
        public bool ShowStockCount { get; set; } = true;

        [Display(Name = "Allow Backorders")]
        public bool AllowBackorders { get; set; } = false;

        // ============================================================
        // Tax Configuration
        // ============================================================

        [Display(Name = "Collect Sales Tax")]
        public bool CollectSalesTax { get; set; } = true;

        [Display(Name = "Tax Rate (%)")]
        [Range(0, 100, ErrorMessage = "Tax rate must be between 0 and 100")]
        public decimal TaxRate { get; set; } = 8.25m;

        [StringLength(200)]
        [Display(Name = "Tax Display Name")]
        public string TaxDisplayName { get; set; } = "Sales Tax";

        [StringLength(500)]
        [Display(Name = "Tax Description")]
        public string? TaxDescription { get; set; } = "State and local sales tax";

        // ============================================================
        // Email Settings
        // ============================================================

        // ============================================================
        // Email Settings - Multi-Provider Support
        // ============================================================

        [Display(Name = "Enable Email Notifications")]
        public bool EnableEmailNotifications { get; set; } = false;

        [Display(Name = "Email Provider")]
        public EmailProvider EmailProvider { get; set; } = EmailProvider.None;

        // Resend Settings
        [StringLength(500)]
        [Display(Name = "Resend API Key")]
        public string? ResendApiKey { get; set; }  // Encrypted

        // Brevo Settings
        [StringLength(500)]
        [Display(Name = "Brevo API Key")]
        public string? BrevoApiKey { get; set; }  // Encrypted

        // SMTP Settings (Gmail, Outlook, custom)
        [StringLength(200)]
        [Display(Name = "SMTP Host")]
        public string? SmtpHost { get; set; }  // e.g., smtp.gmail.com

        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [StringLength(200)]
        [Display(Name = "SMTP Username")]
        public string? SmtpUsername { get; set; }

        [StringLength(500)]
        [Display(Name = "SMTP Password")]
        public string? SmtpPassword { get; set; }  // Encrypted

        [Display(Name = "SMTP Use SSL/TLS")]
        public bool SmtpUseSsl { get; set; } = true;

        // SendGrid (Legacy - for users who already have paid accounts)
        [StringLength(500)]
        [Display(Name = "SendGrid API Key")]
        public string? SendGridApiKey { get; set; }  // Encrypted

        // Common Email Configuration (used by all providers)
        [Required]
        [StringLength(200)]
        [Display(Name = "Email From Name")]
        public string EmailFromName { get; set; } = "My Store";

        [Required]
        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Email From Address")]
        public string EmailFromAddress { get; set; } = "noreply@example.com";

        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Support Email Address")]
        public string EmailSupportAddress { get; set; } = "support@example.com";

        // Email Branding (applies to all providers)
        [StringLength(500)]
        [Display(Name = "Email Logo URL")]
        public string? EmailLogoUrl { get; set; }

        [StringLength(20)]
        [Display(Name = "Email Header Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string EmailHeaderColor { get; set; } = "#0d6efd";  // Primary blue

        [StringLength(20)]
        [Display(Name = "Email Button Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string EmailButtonColor { get; set; } = "#0d6efd";  // Primary blue

        [StringLength(1000)]
        [Display(Name = "Email Footer Text")]
        public string? EmailFooterText { get; set; } = "Thank you for shopping with us!";

        // Email Notification Toggles
        [Display(Name = "Send Order Confirmation Emails")]
        public bool SendOrderConfirmationEmails { get; set; } = true;

        [Display(Name = "Send Shipping Notification Emails")]
        public bool SendShippingNotificationEmails { get; set; } = true;

        [Display(Name = "Send Admin Order Notifications")]
        public bool SendAdminOrderNotifications { get; set; } = true;

        [EmailAddress]
        [StringLength(200)]
        [Display(Name = "Admin Notification Email")]
        public string? AdminNotificationEmail { get; set; }

        // ============================================================
        // Custom CSS/HTML
        // ============================================================

        [Display(Name = "Custom CSS")]
        public string? CustomCss { get; set; }

        [Display(Name = "Custom Header HTML")]
        public string? CustomHeaderHtml { get; set; }

        [Display(Name = "Custom Footer HTML")]
        public string? CustomFooterHtml { get; set; }

        // ============================================================
        // Analytics & Tracking
        // ============================================================

        [Display(Name = "Enable Google Analytics")]
        public bool EnableGoogleAnalytics { get; set; } = false;

        [Display(Name = "Google Analytics Tag (Full Script)")]
        [DataType(DataType.MultilineText)]
        public string? GoogleAnalyticsTag { get; set; }

        [StringLength(50)]
        [Display(Name = "Google Analytics Measurement ID")]
        [RegularExpression(@"^G-[A-Z0-9]+$|^$", ErrorMessage = "Measurement ID must be in format G-XXXXXXXXXX")]
        public string? GoogleAnalyticsMeasurementId { get; set; }

        // ============================================================
        // Metadata
        // ============================================================

        [Required]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? LastModifiedBy { get; set; }

        // ============================================================
        // Helper Methods
        // ============================================================

        /// <summary>
        /// Generates CSS variables for theme colors
        /// </summary>
        public string GenerateThemeCss()
        {
            return $@":root {{
    /* Branding Colors */
    --primary-color: {PrimaryColor};
    --primary-dark: {PrimaryDark};
    --primary-light: {PrimaryLight};
    --secondary-color: {SecondaryColor};
    --accent-color: {AccentColor};
    
    /* Typography */
    --primary-font: {PrimaryFont};
    --heading-font: {HeadingFont};
}}

{CustomCss}";
        }

        /// <summary>
        /// Gets the full page title with site name
        /// </summary>
        public string GetPageTitle(string? pageTitle = null)
        {
            return string.IsNullOrEmpty(pageTitle) 
                ? SiteName 
                : $"{pageTitle} - {SiteName}";
        }
    }

    /// <summary>
    /// Email service provider options for transactional emails
    /// </summary>
    public enum EmailProvider
    {
        /// <summary>
        /// Email notifications disabled
        /// </summary>
        None = 0,

        /// <summary>
        /// Resend - Recommended free option (100 emails/day, no branding)
        /// https://resend.com
        /// </summary>
        Resend = 1,

        /// <summary>
        /// Brevo (formerly Sendinblue) - More emails but has branding (300 emails/day)
        /// https://brevo.com
        /// </summary>
        Brevo = 2,

        /// <summary>
        /// Custom SMTP - Gmail, Outlook, or any SMTP server
        /// Shows "via" tag in some email clients
        /// </summary>
        Smtp = 3,

        /// <summary>
        /// SendGrid - Legacy support (no longer has free tier as of July 2024)
        /// Only for users with existing paid accounts
        /// </summary>
        SendGrid = 4
    }
}
