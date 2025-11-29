using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class BrandingModel : PageModel
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IImageUploadService _imageUploadService;
        private readonly IEncryptionService _encryptionService;
        private readonly IEmailService _emailService;
        private readonly EmailTemplateService _emailTemplateService;
        private readonly IStoredImageService _storedImageService;
        private readonly ILogger<BrandingModel> _logger;

        public BrandingModel(
            ISiteSettingsService siteSettingsService,
            IImageUploadService imageUploadService,
            IEncryptionService encryptionService,
            IEmailService emailService,
            EmailTemplateService emailTemplateService,
            IStoredImageService storedImageService,
            ILogger<BrandingModel> logger)
        {
            _siteSettingsService = siteSettingsService;
            _imageUploadService = imageUploadService;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
            _storedImageService = storedImageService;
            _logger = logger;
        }

        [BindProperty]
        public SiteSettings Settings { get; set; } = default!;

        public string? UploadError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Settings = await _siteSettingsService.GetSettingsAsync();
            return Page();
        }

        // Test email endpoint
        public async Task<IActionResult> OnPostTestEmailAsync([FromBody] TestEmailRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Email))
                {
                    return new JsonResult(new { success = false, message = "Email address is required" });
                }

                var result = await _emailService.SendTestEmailAsync(request.Email);

                if (result)
                {
                    return new JsonResult(new { success = true, message = "Test email sent successfully!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Failed to send test email. Check configuration and logs." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return new JsonResult(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Preview email without sending
        public async Task<IActionResult> OnPostPreviewEmailAsync([FromBody] PreviewEmailRequest request)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                // Create sample order for preview
                var sampleOrder = new Order 
                { 
                    OrderNumber = "PREVIEW-001",
                    OrderDate = DateTime.Now,
                    ShippingName = "John Doe",
                    ShippingAddress = "123 Main St",
                    ShippingCity = "Springfield",
                    ShippingState = "IL",
                    ShippingZip = "62701",
                    Subtotal = 99.99m,
                    TaxAmount = 8.25m,
                    TotalAmount = 108.24m,
                    OrderItems = new List<OrderItem>
                    {
                        new OrderItem { 
                            Product = new Product { Name = "Sample Product" }, 
                            Quantity = 1, 
                            UnitPrice = 99.99m
                        }
                    }
                };
                
                string htmlContent = request.EmailType switch
                {
                    "order" => await _emailTemplateService.GenerateOrderConfirmation(sampleOrder, settings),
                    
                    "shipping" => await _emailTemplateService.GenerateShippingNotification(sampleOrder, "1Z999AA10123456784", settings),
                    
                    "welcome" => await _emailTemplateService.GenerateWelcomeEmail(new ApplicationUser 
                    { 
                        Email = "preview@example.com" 
                    }, settings),
                    
                    _ => await _emailTemplateService.GenerateTestEmail(settings)
                };

                // Make URLs absolute by prepending the domain
                var request_context = HttpContext.Request;
                var baseUrl = $"{request_context.Scheme}://{request_context.Host}";
                htmlContent = htmlContent.Replace("src=\"/", $"src=\"{baseUrl}/");

                // Replace cid:logo with actual base64 data URI for preview (CID only works in emails)
                if (htmlContent.Contains("cid:logo"))
                {
                    Guid? imageId = settings.EmailLogoImageId ?? settings.LogoImageId;
                    if (imageId.HasValue && imageId.Value != Guid.Empty)
                    {
                        var base64DataUri = await _storedImageService.GetImageAsBase64DataUriAsync(imageId.Value);
                        if (!string.IsNullOrEmpty(base64DataUri))
                        {
                            htmlContent = htmlContent.Replace("cid:logo", base64DataUri);
                        }
                    }
                }

                return new JsonResult(new { success = true, html = htmlContent });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing email");
                return new JsonResult(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // AJAX endpoint for instant logo upload
        public async Task<IActionResult> OnPostUploadLogoAsync(IFormFile logoFile)
        {
            try
            {
                if (logoFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                if (!_imageUploadService.IsValidImage(logoFile))
                    return new JsonResult(new { success = false, message = "Invalid file. Use JPG, PNG, GIF, or SVG (max 5MB)" });

                // Save image to StoredImages table (encrypted)
                var imageId = await _storedImageService.SaveLocalImageAsync(
                    logoFile, 
                    "branding", 
                    "SiteSettings:Logo", 
                    User.Identity?.Name
                );
                
                // Update settings with FK to StoredImage
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.LogoImageId = imageId;
                settings.LogoUrl = null;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                var imageUrl = $"/images/stored/{imageId}";

                return new JsonResult(new { success = true, message = "Logo uploaded successfully!", url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        // AJAX endpoint for instant horizontal logo upload
        public async Task<IActionResult> OnPostUploadHorizontalLogoAsync(IFormFile horizontalLogoFile)
        {
            try
            {
                if (horizontalLogoFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                if (!_imageUploadService.IsValidImage(horizontalLogoFile))
                    return new JsonResult(new { success = false, message = "Invalid file. Use JPG, PNG, GIF, or SVG (max 5MB)" });

                // Save image to StoredImages table (encrypted)
                var imageId = await _storedImageService.SaveLocalImageAsync(
                    horizontalLogoFile, 
                    "branding", 
                    "SiteSettings:HorizontalLogo", 
                    User.Identity?.Name
                );
                
                // Update settings with FK to StoredImage
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.HorizontalLogoImageId = imageId;
                settings.HorizontalLogoUrl = null;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                var imageUrl = $"/images/stored/{imageId}";

                return new JsonResult(new { success = true, message = "Horizontal logo uploaded successfully!", url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading horizontal logo");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        // AJAX endpoint for instant favicon upload
        public async Task<IActionResult> OnPostUploadFaviconAsync(IFormFile faviconFile)
        {
            try
            {
                if (faviconFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                if (!_imageUploadService.IsValidImage(faviconFile))
                    return new JsonResult(new { success = false, message = "Invalid file. Use ICO, PNG, or SVG (max 5MB)" });

                // Save image to StoredImages table (encrypted)
                var imageId = await _storedImageService.SaveLocalImageAsync(
                    faviconFile, 
                    "branding", 
                    "SiteSettings:Favicon", 
                    User.Identity?.Name
                );
                
                // Update settings with FK to StoredImage
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.FaviconImageId = imageId;
                settings.FaviconUrl = null;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                var imageUrl = $"/images/stored/{imageId}";
                _logger.LogInformation("Favicon uploaded to StoredImages with ID {Id}", imageId);

                return new JsonResult(new { success = true, message = "Favicon uploaded successfully!", url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading favicon");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        // AJAX endpoint for instant hero image upload
        public async Task<IActionResult> OnPostUploadHeroImageAsync(IFormFile heroImageFile)
        {
            try
            {
                if (heroImageFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                if (!_imageUploadService.IsValidImage(heroImageFile))
                    return new JsonResult(new { success = false, message = "Invalid file. Use JPG, PNG, GIF, or SVG (max 5MB)" });

                // Save image to StoredImages table (encrypted)
                var imageId = await _storedImageService.SaveLocalImageAsync(
                    heroImageFile, 
                    "branding", 
                    "SiteSettings:HeroImage", 
                    User.Identity?.Name
                );
                
                // Update settings with FK to StoredImage
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.HeroImageId = imageId;
                settings.HeroImageUrl = null;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                var imageUrl = $"/images/stored/{imageId}";
                _logger.LogInformation("Hero image uploaded to StoredImages with ID {Id}", imageId);

                return new JsonResult(new { success = true, message = "Hero image uploaded successfully!", url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading hero image");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        // AJAX endpoint for instant email logo upload
        public async Task<IActionResult> OnPostUploadEmailLogoAsync(IFormFile emailLogoFile)
        {
            try
            {
                if (emailLogoFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                if (!_imageUploadService.IsValidImage(emailLogoFile))
                    return new JsonResult(new { success = false, message = "Invalid file. Use JPG, PNG, GIF, or SVG (max 5MB)" });

                // Save image to StoredImages table (encrypted)
                var imageId = await _storedImageService.SaveLocalImageAsync(
                    emailLogoFile, 
                    "branding", 
                    "SiteSettings:EmailLogo", 
                    User.Identity?.Name
                );
                
                // Update settings with FK to StoredImage
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.EmailLogoImageId = imageId;
                settings.EmailLogoUrl = null;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                var imageUrl = $"/images/stored/{imageId}";

                return new JsonResult(new { success = true, message = "Email logo uploaded successfully!", url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading email logo");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the validation errors and try again.";
                return Page();
            }

            try
            {
                // Get the ORIGINAL settings from database to compare
                var originalSettings = await _siteSettingsService.GetSettingsAsync();
                
                _logger.LogInformation("Starting to save settings. Checking for changes...");

                // Only encrypt if the value has CHANGED from what's in the database
                // This prevents re-encrypting already encrypted values
                
                // NOTE: ResendApiKey and BrevoApiKey moved to ApiConfigurations table
                // Use Admin > Settings > API Keys to manage email API keys

                // Handle SmtpPassword
                if (!string.IsNullOrEmpty(Settings.SmtpPassword))
                {
                    // If the value is different from what's in DB, it's a new value that needs encryption
                    if (Settings.SmtpPassword != originalSettings.SmtpPassword)
                    {
                        _logger.LogInformation("SmtpPassword has changed, attempting to encrypt...");
                        try
                        {
                            Settings.SmtpPassword = _encryptionService.Encrypt(Settings.SmtpPassword);
                            _logger.LogInformation("? SmtpPassword encrypted successfully");
                        }
                        catch (Exception encEx)
                        {
                            _logger.LogWarning(encEx, "?? Failed to encrypt SmtpPassword - saving unencrypted. Configure ENCRYPTION_KEY environment variable for production.");
                            // Continue without encryption - this is OK for development
                        }
                    }
                    else
                    {
                        _logger.LogInformation("SmtpPassword unchanged, keeping existing value");
                    }
                }

                // Note: Logo, Favicon, Hero Image, and Horizontal Logo are uploaded via AJAX
                // Those fields should NOT be overwritten by this form submission
                // Preserve the existing image URLs and FK columns from database
                Settings.LogoUrl = originalSettings.LogoUrl;
                Settings.LogoImageId = originalSettings.LogoImageId;
                Settings.FaviconUrl = originalSettings.FaviconUrl;
                Settings.FaviconImageId = originalSettings.FaviconImageId;
                Settings.HorizontalLogoUrl = originalSettings.HorizontalLogoUrl;
                Settings.HorizontalLogoImageId = originalSettings.HorizontalLogoImageId;
                Settings.HeroImageUrl = originalSettings.HeroImageUrl;
                Settings.HeroImageId = originalSettings.HeroImageId;
                Settings.EmailLogoUrl = originalSettings.EmailLogoUrl;
                Settings.EmailLogoImageId = originalSettings.EmailLogoImageId;
                
                var userEmail = User.Identity?.Name;
                
                _logger.LogInformation("About to save settings for user {User}", userEmail);
                await _siteSettingsService.UpdateSettingsAsync(Settings, userEmail);
                _logger.LogInformation("? Settings saved successfully for user {User}", userEmail);

                TempData["SuccessMessage"] = "Branding settings saved successfully! Changes are now live.";
                _logger.LogInformation("Branding settings updated by {User}", userEmail);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error saving branding settings");
                TempData["ErrorMessage"] = $"Failed to save branding settings: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResetToDefaultsAsync()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                await _siteSettingsService.ResetToDefaultsAsync(userEmail);

                TempData["SuccessMessage"] = "Branding settings reset to defaults (Mushroom theme).";
                _logger.LogInformation("Branding settings reset to defaults by {User}", userEmail);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting branding settings");
                TempData["ErrorMessage"] = "Failed to reset branding settings. Please try again.";
                return RedirectToPage();
            }
        }

        // Get current timezone settings
        public async Task<IActionResult> OnGetTimezoneAsync()
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                TimeZoneInfo? currentTimeZone = null;

                try
                {
                    currentTimeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
                }
                catch
                {
                    // Fall back to system local time if configured timezone is invalid
                    currentTimeZone = TimeZoneInfo.Local;
                }

                var currentDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, currentTimeZone);
                var utcOffset = currentTimeZone.GetUtcOffset(DateTime.UtcNow);
                var offsetString = $"UTC{(utcOffset >= TimeSpan.Zero ? "+" : "")}{utcOffset.Hours:D2}:{utcOffset.Minutes:D2}";

                return new JsonResult(new
                {
                    timeZoneId = settings.TimeZoneId,
                    displayName = currentTimeZone.DisplayName,
                    currentUtcOffset = offsetString,
                    currentDateTime = currentDateTime.ToString("o")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timezone settings");
                return new JsonResult(new { success = false, message = "Error retrieving timezone settings" });
            }
        }

        // Get available timezones
        public IActionResult OnGetAvailableTimezonesAsync()
        {
            try
            {
                var timezones = TimeZoneInfo.GetSystemTimeZones()
                    .Select(tz => new
                    {
                        id = tz.Id,
                        displayName = tz.DisplayName,
                        baseUtcOffset = tz.BaseUtcOffset.ToString(@"hh\:mm"),
                        supportsDaylightSavingTime = tz.SupportsDaylightSavingTime
                    })
                    .OrderBy(tz => TimeZoneInfo.FindSystemTimeZoneById(tz.id).BaseUtcOffset)
                    .ThenBy(tz => tz.displayName)
                    .ToList();

                return new JsonResult(timezones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available timezones");
                return new JsonResult(new { success = false, message = "Error retrieving timezones" });
            }
        }

        // Update timezone
        public async Task<IActionResult> OnPostUpdateTimezoneAsync([FromBody] UpdateTimezoneRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.TimeZoneId))
                {
                    return new JsonResult(new { success = false, message = "Timezone ID is required" });
                }

                // Validate timezone ID
                TimeZoneInfo timeZone;
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return new JsonResult(new { success = false, message = "Invalid timezone ID" });
                }

                // Update settings
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.TimeZoneId = request.TimeZoneId;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                _logger.LogInformation("Timezone updated to {TimeZoneId} by {User}", request.TimeZoneId, User.Identity?.Name);

                return new JsonResult(new
                {
                    success = true,
                    message = "Timezone updated successfully",
                    timeZoneId = settings.TimeZoneId,
                    displayName = timeZone.DisplayName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timezone");
                return new JsonResult(new { success = false, message = "Error updating timezone" });
            }
        }
    }

    public class TestEmailRequest
    {
        public string? Email { get; set; }
    }

    public class PreviewEmailRequest
    {
        public string? EmailType { get; set; } // "order", "shipping", "welcome", "test"
    }

    public class UpdateTimezoneRequest
    {
        public string? TimeZoneId { get; set; }
    }
}






