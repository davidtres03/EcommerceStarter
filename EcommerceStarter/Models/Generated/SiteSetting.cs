using System;
using System.Collections.Generic;

namespace EcommerceStarter.Models.Generated;

public partial class SiteSetting
{
    public int Id { get; set; }

    public string SiteName { get; set; } = null!;

    public string SiteTagline { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? FaviconUrl { get; set; }

    public string? SiteIcon { get; set; }

    public string PrimaryColor { get; set; } = null!;

    public string PrimaryDark { get; set; } = null!;

    public string PrimaryLight { get; set; } = null!;

    public string SecondaryColor { get; set; } = null!;

    public string AccentColor { get; set; } = null!;

    public string PrimaryFont { get; set; } = null!;

    public string HeadingFont { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string ContactEmail { get; set; } = null!;

    public string SupportEmail { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? FacebookUrl { get; set; }

    public string? TwitterUrl { get; set; }

    public string? InstagramUrl { get; set; }

    public string? LinkedInUrl { get; set; }

    public string? YouTubeUrl { get; set; }

    public string MetaDescription { get; set; } = null!;

    public string? MetaKeywords { get; set; }

    public bool EnableGuestCheckout { get; set; }

    public bool EnableProductReviews { get; set; }

    public bool EnableWishlist { get; set; }

    public bool ShowStockCount { get; set; }

    public bool AllowBackorders { get; set; }

    public string EmailFromName { get; set; } = null!;

    public string EmailFromAddress { get; set; } = null!;

    public string? CustomCss { get; set; }

    public string? CustomHeaderHtml { get; set; }

    public string? CustomFooterHtml { get; set; }

    public DateTime LastModified { get; set; }

    public string? LastModifiedBy { get; set; }

    public string? HeroImageUrl { get; set; }

    public string? AdminNotificationEmail { get; set; }

    public string EmailButtonColor { get; set; } = null!;

    public string? EmailFooterText { get; set; }

    public string EmailHeaderColor { get; set; } = null!;

    public string? EmailLogoUrl { get; set; }

    public string EmailSupportAddress { get; set; } = null!;

    public bool EnableEmailNotifications { get; set; }

    public bool SendAdminOrderNotifications { get; set; }

    public string? SendGridApiKey { get; set; }

    public bool SendOrderConfirmationEmails { get; set; }

    public bool SendShippingNotificationEmails { get; set; }

    public string? BrevoApiKey { get; set; }

    public int EmailProvider { get; set; }

    public string? ResendApiKey { get; set; }

    public string? SmtpHost { get; set; }

    public string? SmtpPassword { get; set; }

    public int SmtpPort { get; set; }

    public bool SmtpUseSsl { get; set; }

    public string? SmtpUsername { get; set; }

    public bool EnableGoogleAnalytics { get; set; }

    public string? GoogleAnalyticsMeasurementId { get; set; }

    public string? GoogleAnalyticsTag { get; set; }
}
