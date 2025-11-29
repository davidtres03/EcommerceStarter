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

        [Display(Name = "Logo Image ID (Stored)")]
        public Guid? LogoImageId { get; set; }
        
        public StoredImage? LogoImage { get; set; }

        [StringLength(500)]
        [Display(Name = "Horizontal Logo URL")]
        public string? HorizontalLogoUrl { get; set; } = "/logo-horizontal.svg";

        [Display(Name = "Horizontal Logo Image ID (Stored)")]
        public Guid? HorizontalLogoImageId { get; set; }
        
        public StoredImage? HorizontalLogoImage { get; set; }

        [StringLength(500)]
        [Display(Name = "Favicon URL")]
        public string? FaviconUrl { get; set; } = "/favicon.ico";

        [Display(Name = "Favicon Image ID (Stored)")]
        public Guid? FaviconImageId { get; set; }
        
        public StoredImage? FaviconImage { get; set; }

        [StringLength(500)]
        [Display(Name = "Hero Image URL")]
        public string? HeroImageUrl { get; set; } = "/images/hero-bg.jpg";

        [Display(Name = "Hero Image ID (Stored)")]
        public Guid? HeroImageId { get; set; }
        
        public StoredImage? HeroImage { get; set; }

        [StringLength(50)]
        [Display(Name = "Site Icon/Emoji")]
        public string? SiteIcon { get; set; } = "??";  // Shopping cart emoji - generic e-commerce

        // ============================================================
        // Hero Section Customization (Homepage)
        // ============================================================

        [StringLength(200)]
        [Display(Name = "Hero Title")]
        public string? HeroTitle { get; set; }  // Falls back to SiteName if null

        [StringLength(500)]
        [Display(Name = "Hero Subtitle")]
        public string? HeroSubtitle { get; set; }  // Falls back to SiteTagline if null

        [StringLength(100)]
        [Display(Name = "Hero Badge Text")]
        public string? HeroBadgeText { get; set; }  // Falls back to CompanyName if null

        [Required]
        [StringLength(100)]
        [Display(Name = "Primary Button Text")]
        public string HeroPrimaryButtonText { get; set; } = "Shop Collection";

        [Required]
        [StringLength(200)]
        [Display(Name = "Primary Button Link")]
        public string HeroPrimaryButtonLink { get; set; } = "/Products/Index";

        [StringLength(100)]
        [Display(Name = "Secondary Button Text")]
        public string? HeroSecondaryButtonText { get; set; } = "Our Story";

        [StringLength(200)]
        [Display(Name = "Secondary Button Link")]
        public string? HeroSecondaryButtonLink { get; set; } = "/About";

        [Display(Name = "Show Hero Features")]
        public bool ShowHeroFeatures { get; set; } = true;

        [StringLength(100)]
        [Display(Name = "Feature 1 Text")]
        public string HeroFeature1Text { get; set; } = "Free Shipping $50+";

        [StringLength(50)]
        [Display(Name = "Feature 1 Icon")]
        public string HeroFeature1Icon { get; set; } = "bi-truck";

        [StringLength(100)]
        [Display(Name = "Feature 2 Text")]
        public string HeroFeature2Text { get; set; } = "Sustainable Materials";

        [StringLength(50)]
        [Display(Name = "Feature 2 Icon")]
        public string HeroFeature2Icon { get; set; } = "bi-shield-check";

        [StringLength(100)]
        [Display(Name = "Feature 3 Text")]
        public string HeroFeature3Text { get; set; } = "30-Day Returns";

        [StringLength(50)]
        [Display(Name = "Feature 3 Icon")]
        public string HeroFeature3Icon { get; set; } = "bi-arrow-counterclockwise";

        [Display(Name = "Show Scroll Indicator")]
        public bool ShowScrollIndicator { get; set; } = true;

        // ============================================================
        // Theme Colors (Bootstrap 5 Defaults - Neutral Blue Theme)
        // ============================================================

        [Required]
        [StringLength(20)]
        [Display(Name = "Primary Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format (e.g., #0d6efd)")]
        public string PrimaryColor { get; set; } = "#2563EB";  // Modern professional blue

        [Required]
        [StringLength(20)]
        [Display(Name = "Primary Dark")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string PrimaryDark { get; set; } = "#1E40AF";  // Deep professional blue

        [Required]
        [StringLength(20)]
        [Display(Name = "Primary Light")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string PrimaryLight { get; set; } = "#60A5FA";  // Light professional blue

        [Required]
        [StringLength(20)]
        [Display(Name = "Secondary Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string SecondaryColor { get; set; } = "#64748B";  // Slate gray (professional neutral)

        [Required]
        [StringLength(20)]
        [Display(Name = "Accent Color")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Color must be in hex format")]
        public string AccentColor { get; set; } = "#10B981";  // Success green (positive, clear)

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
        // UI Design System (Professional Customization)
        // ============================================================

        [Display(Name = "Button Style")]
        public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Solid;

        [Display(Name = "Card Style")]
        public CardStyle CardStyle { get; set; } = CardStyle.Shadow;

        [Display(Name = "Navigation Style")]
        public NavigationStyle NavigationStyle { get; set; } = NavigationStyle.Classic;

        [Display(Name = "Corner Rounding")]
        public CornerRounding CornerRounding { get; set; } = CornerRounding.Moderate;

        [Display(Name = "Spacing Density")]
        public SpacingDensity SpacingDensity { get; set; } = SpacingDensity.Default;

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
        // Regional Settings
        // ============================================================

        [StringLength(100)]
        [Display(Name = "Timezone")]
        public string TimeZoneId { get; set; } = "Central Standard Time";  // Auto-detected on first run

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
        // NOTE: API keys are stored in ApiConfigurations table (not here)

        [Display(Name = "Enable Email Notifications")]
        public bool EnableEmailNotifications { get; set; } = false;

        [Display(Name = "Email Provider")]
        public EmailProvider EmailProvider { get; set; } = EmailProvider.None;

        // Foreign key reference to ApiConfigurations table (if email provider requires API key)
        public int? ApiConfigurationId { get; set; }

        // SMTP Settings (Gmail, Outlook, custom) - No API key needed
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

        [Display(Name = "Email Logo Image ID (Stored)")]
        public Guid? EmailLogoImageId { get; set; }
        
        public StoredImage? EmailLogoImage { get; set; }

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
        // Analytics & Tracking (Cloudflare Google Tag Gateway)
        // ============================================================

        [StringLength(50)]
        [Display(Name = "Google Tag ID (GTM or GA4)")]
        [RegularExpression(@"^(GTM-[A-Z0-9]+|G-[A-Z0-9]+)$|^$", ErrorMessage = "Must be GTM-XXXXXXX or G-XXXXXXXXXX format")]
        public string? GoogleAnalyticsMeasurementId { get; set; }

        [StringLength(100)]
        [Display(Name = "Cloudflare Gateway Measurement Path")]
        [RegularExpression(@"^/[a-zA-Z0-9/]+$|^$", ErrorMessage = "Must start with / and contain only letters, numbers, and slashes")]
        public string? MeasurementPath { get; set; } = "/metrics";

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
            // Convert enums to CSS values
            var borderRadius = CornerRounding switch
            {
                CornerRounding.Sharp => "4px",
                CornerRounding.Moderate => "8px",
                CornerRounding.Rounded => "12px",
                CornerRounding.ExtraRounded => "16px",
                _ => "8px"
            };

            var spacing = SpacingDensity switch
            {
                SpacingDensity.Compact => "0.75rem",
                SpacingDensity.Default => "1rem",
                SpacingDensity.Spacious => "1.5rem",
                _ => "1rem"
            };

            return $@"/* ========================================
   ADMIN-CONFIGURED THEME OVERRIDES
   These CSS variables override the defaults in site.css
   Configured via Admin Panel > Settings > Branding
   ======================================== */
:root {{
    /* Branding Colors - Override from Admin Settings */
    --primary-color: {PrimaryColor};
    --primary-dark: {PrimaryDark};
    --primary-light: {PrimaryLight};
    --secondary-color: {SecondaryColor};
    --accent-color: {AccentColor};
    
    /* Background Colors */
    --bg-white: #ffffff;
    
    /* Typography - Override from Admin Settings */
    --primary-font: {(string.IsNullOrEmpty(PrimaryFont) ? "'Segoe UI', system-ui, sans-serif" : PrimaryFont)};
    --heading-font: {(string.IsNullOrEmpty(HeadingFont) ? "var(--primary-font)" : HeadingFont)};
    
    /* UI Design System */
    --border-radius: {borderRadius};
    --spacing-unit: {spacing};
}}

/* Typography Application */
body {{
    font-family: var(--primary-font);
}}

h1, h2, h3, h4, h5, h6 {{
    font-family: var(--heading-font);
}}

/* Button Styles Based on ButtonStyle Setting */
{GenerateButtonStyles()}

/* Card Styles Based on CardStyle Setting */
{GenerateCardStyles()}

/* Navigation Styles Based on NavigationStyle Setting */
{GenerateNavigationStyles()}

/* ========================================
   CUSTOM CSS FROM ADMIN PANEL
   ======================================== */
{CustomCss}";
        }

        private string GenerateNavigationStyles()
        {
            return NavigationStyle switch
            {
                NavigationStyle.Classic => @"
/* Classic: Default left-aligned navigation */
.navbar .navbar-collapse {
    justify-content: flex-start !important;
}",
                NavigationStyle.Centered => @"
/* Centered: Logo on left, menu centered */
.navbar .navbar-collapse {
    justify-content: center !important;
}
.navbar .navbar-collapse .navbar-nav {
    margin: 0 auto;
}",
                NavigationStyle.Minimal => @"
/* Minimal: Compact spacing */
.navbar {
    padding-top: 0.25rem;
    padding-bottom: 0.25rem;
}
.navbar .nav-link {
    padding-left: 0.5rem;
    padding-right: 0.5rem;
}",
                NavigationStyle.Split => @"
/* Split: Menu items pushed to right */
.navbar .navbar-collapse {
    justify-content: flex-end !important;
}",
                _ => ""
            };
        }

        private string GenerateButtonStyles()
        {
            return ButtonStyle switch
            {
                ButtonStyle.Solid => @"
.btn-theme-primary {
    background: var(--primary-color);
    color: white;
    border: 2px solid var(--primary-color);
}
.btn-theme-primary:hover {
    background: var(--primary-dark);
    border-color: var(--primary-dark);
}",
                ButtonStyle.Outline => @"
.btn-theme-primary {
    background: transparent;
    color: var(--primary-color);
    border: 2px solid var(--primary-color);
}
.btn-theme-primary:hover {
    background: var(--primary-color);
    color: white;
    border-color: var(--primary-color);
}",
                ButtonStyle.Ghost => @"
.btn-theme-primary {
    background: transparent;
    color: var(--primary-color);
    border: 2px solid transparent;
}
.btn-theme-primary:hover {
    background: rgba(var(--primary-color-rgb), 0.1);
    border-color: transparent;
}",
                ButtonStyle.Soft => @"
.btn-theme-primary {
    background: rgba(var(--primary-color-rgb), 0.15);
    color: var(--primary-color);
    border: 2px solid transparent;
}
.btn-fungal-primary:hover {
    background: rgba(var(--primary-color-rgb), 0.25);
    border-color: transparent;
}",
                _ => ""
            };
        }

        private string GenerateCardStyles()
        {
            return CardStyle switch
            {
                CardStyle.Bordered => @"
.product-card-fungal {
    border: 1px solid #e0e0e0;
    box-shadow: none;
}",
                CardStyle.Shadow => @"
.product-card-fungal {
    border: none;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}",
                CardStyle.Elevated => @"
.product-card-fungal {
    border: none;
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
}",
                CardStyle.Flat => @"
.product-card-fungal {
    border: none;
    box-shadow: none;
}",
                _ => ""
            };
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

    /// <summary>
    /// Button style options for professional UI customization
    /// </summary>
    public enum ButtonStyle
    {
        /// <summary>
        /// Solid filled buttons (default, most prominent)
        /// </summary>
        Solid = 0,

        /// <summary>
        /// Outline/bordered buttons (subtle, modern)
        /// </summary>
        Outline = 1,

        /// <summary>
        /// Ghost/text-only buttons (minimal, clean)
        /// </summary>
        Ghost = 2,

        /// <summary>
        /// Soft/pastel background buttons (gentle, approachable)
        /// </summary>
        Soft = 3
    }

    /// <summary>
    /// Card style options for content containers
    /// </summary>
    public enum CardStyle
    {
        /// <summary>
        /// Bordered cards with subtle border (minimal)
        /// </summary>
        Bordered = 0,

        /// <summary>
        /// Shadow cards with elevation (default, modern)
        /// </summary>
        Shadow = 1,

        /// <summary>
        /// Elevated cards with larger shadow (prominent)
        /// </summary>
        Elevated = 2,

        /// <summary>
        /// Flat cards with no border or shadow (ultra-minimal)
        /// </summary>
        Flat = 3
    }

    /// <summary>
    /// Navigation bar style options
    /// </summary>
    public enum NavigationStyle
    {
        /// <summary>
        /// Classic left-aligned navigation (default)
        /// </summary>
        Classic = 0,

        /// <summary>
        /// Centered navigation (modern, symmetric)
        /// </summary>
        Centered = 1,

        /// <summary>
        /// Minimal compact navigation (space-efficient)
        /// </summary>
        Minimal = 2,

        /// <summary>
        /// Split navigation (logo left, menu right)
        /// </summary>
        Split = 3
    }

    /// <summary>
    /// Corner rounding/border radius options
    /// </summary>
    public enum CornerRounding
    {
        /// <summary>
        /// Sharp corners with minimal rounding (4px - modern, crisp)
        /// </summary>
        Sharp = 0,

        /// <summary>
        /// Moderate rounding (8px - default, balanced)
        /// </summary>
        Moderate = 1,

        /// <summary>
        /// Rounded corners (12px - friendly, soft)
        /// </summary>
        Rounded = 2,

        /// <summary>
        /// Extra rounded (16px - playful, distinct)
        /// </summary>
        ExtraRounded = 3
    }

    /// <summary>
    /// Spacing density options for layout
    /// </summary>
    public enum SpacingDensity
    {
        /// <summary>
        /// Compact spacing (information-dense)
        /// </summary>
        Compact = 0,

        /// <summary>
        /// Default spacing (balanced)
        /// </summary>
        Default = 1,

        /// <summary>
        /// Spacious (breathable, relaxed)
        /// </summary>
        Spacious = 2
    }
}

