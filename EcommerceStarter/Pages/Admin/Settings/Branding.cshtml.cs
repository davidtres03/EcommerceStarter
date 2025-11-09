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
        private readonly ILogger<BrandingModel> _logger;

        public BrandingModel(
            ISiteSettingsService siteSettingsService,
            IImageUploadService imageUploadService,
            IEncryptionService encryptionService,
            IEmailService emailService,
            EmailTemplateService emailTemplateService,
            ILogger<BrandingModel> logger)
        {
            _siteSettingsService = siteSettingsService;
            _imageUploadService = imageUploadService;
            _encryptionService = encryptionService;
            _emailService = emailService;
            _emailTemplateService = emailTemplateService;
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
                    "order" => _emailTemplateService.GenerateOrderConfirmation(sampleOrder, settings),
                    
                    "shipping" => _emailTemplateService.GenerateShippingNotification(sampleOrder, "1Z999AA10123456784", settings),
                    
                    "welcome" => _emailTemplateService.GenerateWelcomeEmail(new ApplicationUser 
                    { 
                        Email = "preview@example.com" 
                    }, settings),
                    
                    _ => _emailTemplateService.GenerateTestEmail(settings)
                };

                // Make URLs absolute by prepending the domain
                var request_context = HttpContext.Request;
                var baseUrl = $"{request_context.Scheme}://{request_context.Host}";
                htmlContent = htmlContent.Replace("src=\"/", $"src=\"{baseUrl}/");

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

                var logoUrl = await _imageUploadService.UploadImageAsync(logoFile, "images/branding");
                
                // Update settings in database
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.LogoUrl = logoUrl;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                _logger.LogInformation("Logo uploaded via AJAX: {Url}", logoUrl);

                return new JsonResult(new { success = true, message = "Logo uploaded successfully!", url = logoUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading logo");
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

                var faviconUrl = await _imageUploadService.UploadImageAsync(faviconFile, "images/branding");
                
                // Update settings in database
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.FaviconUrl = faviconUrl;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                _logger.LogInformation("Favicon uploaded via AJAX: {Url}", faviconUrl);

                return new JsonResult(new { success = true, message = "Favicon uploaded successfully!", url = faviconUrl });
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

                var heroImageUrl = await _imageUploadService.UploadImageAsync(heroImageFile, "images/branding");
                
                // Update settings in database
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.HeroImageUrl = heroImageUrl;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                _logger.LogInformation("Hero image uploaded via AJAX: {Url}", heroImageUrl);

                return new JsonResult(new { success = true, message = "Hero image uploaded successfully!", url = heroImageUrl });
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

                var emailLogoUrl = await _imageUploadService.UploadImageAsync(emailLogoFile, "images/branding");
                
                // Update settings in database
                var settings = await _siteSettingsService.GetSettingsAsync();
                settings.EmailLogoUrl = emailLogoUrl;
                await _siteSettingsService.UpdateSettingsAsync(settings, User.Identity?.Name);

                _logger.LogInformation("Email logo uploaded via AJAX: {Url}", emailLogoUrl);

                return new JsonResult(new { success = true, message = "Email logo uploaded successfully!", url = emailLogoUrl });
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
                
                // Handle ResendApiKey
                if (!string.IsNullOrEmpty(Settings.ResendApiKey))
                {
                    // If the value is different from what's in DB, it's a new value that needs encryption
                    if (Settings.ResendApiKey != originalSettings.ResendApiKey)
                    {
                        _logger.LogInformation("ResendApiKey has changed, attempting to encrypt...");
                        try
                        {
                            Settings.ResendApiKey = _encryptionService.Encrypt(Settings.ResendApiKey);
                            _logger.LogInformation("? ResendApiKey encrypted successfully");
                        }
                        catch (Exception encEx)
                        {
                            _logger.LogWarning(encEx, "?? Failed to encrypt ResendApiKey - saving unencrypted. Configure ENCRYPTION_KEY environment variable for production.");
                            // Continue without encryption - this is OK for development
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ResendApiKey unchanged, keeping existing value");
                    }
                }

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

                // Note: Logo, Favicon, and Hero Image are now uploaded via AJAX
                // This handler only saves color, font, text, and email settings changes
                
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
    }

    public class TestEmailRequest
    {
        public string? Email { get; set; }
    }

    public class PreviewEmailRequest
    {
        public string? EmailType { get; set; } // "order", "shipping", "welcome", "test"
    }
}



