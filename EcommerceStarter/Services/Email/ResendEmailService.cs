using EcommerceStarter.Models;
using Resend;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Resend email service implementation using direct HTTP API
    /// Best free option: 100 emails/day, no branding, professional appearance
    /// </summary>
    public class ResendEmailService : IEmailService
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IEncryptionService _encryptionService;
        private readonly EmailTemplateService _templateService;
        private readonly IStoredImageService _storedImageService;
        private readonly ILogger<ResendEmailService> _logger;
        private readonly IApiConfigurationService _apiConfigService;

        public ResendEmailService(
            ISiteSettingsService siteSettingsService,
            IEncryptionService encryptionService,
            EmailTemplateService templateService,
            IStoredImageService storedImageService,
            ILogger<ResendEmailService> logger,
            IApiConfigurationService apiConfigService)
        {
            _siteSettingsService = siteSettingsService;
            _encryptionService = encryptionService;
            _templateService = templateService;
            _storedImageService = storedImageService;
            _logger = logger;
            _apiConfigService = apiConfigService;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            // Check if Resend is enabled and ApiConfigurationId is set
            return settings.EnableEmailNotifications && 
                   settings.EmailProvider == EmailProvider.Resend && 
                   settings.ApiConfigurationId.HasValue &&
                   settings.ApiConfigurationId.Value > 0;
        }

        private async Task<string?> GetApiKeyAsync()
        {
            // Read from ApiConfigurations (DB-only)
            try
            {
                var apiType = "Email";
                var configs = await _apiConfigService.GetConfigurationsByTypeAsync(apiType, false);
                var config = configs.FirstOrDefault(c => c.IsActive);
                
                if (config == null)
                {
                    _logger.LogWarning("No active Email configuration found in database");
                    return null;
                }
                
                var values = await _apiConfigService.GetDecryptedValuesAsync(config.Id);
                var apiKey = values.GetValueOrDefault("Value1");
                return apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read Resend API key from database");
                return null;
            }
        }

        private async Task<bool> SendEmailViaResendAsync(string from, string to, string subject, string htmlBody)
        {
            try
            {
                var apiKey = await GetApiKeyAsync();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return false;
                }

                // Create ResendClient with API key using static factory method
                var options = new ResendClientOptions
                {
                    ApiToken = apiKey
                };
                var resend = ResendClient.Create(options);

                var message = new EmailMessage
                {
                    From = from,
                    To = new[] { to },
                    Subject = subject,
                    HtmlBody = htmlBody
                };

                // Check if we need to attach logo for CID embedding
                var settings = await _siteSettingsService.GetSettingsAsync();
                await AttachLogoIfNeeded(message, settings, htmlBody);

                var response = await resend.EmailSendAsync(message);
                
                if (response.Content != Guid.Empty)
                {
                    _logger.LogInformation($"Email sent successfully via Resend API. Email ID: {response.Content}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Resend API returned empty email ID");
                    return false;
                }
            }
            catch (ResendException ex)
            {
                _logger.LogError(ex, $"Resend API error: {ex.ErrorType} - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Resend API");
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationAsync(Order order)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (!settings.EnableEmailNotifications || !settings.SendOrderConfirmationEmails)
                {
                    _logger.LogInformation("Order confirmation emails are disabled");
                    return false;
                }

                var htmlContent = await _templateService.GenerateOrderConfirmation(order, settings);
                var subject = $"Order Confirmation - {order.OrderNumber}";

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    order.CustomerEmail,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Order confirmation email sent via Resend for order {order.OrderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending order confirmation via Resend for order {order.OrderNumber}");
                return false;
            }
        }

        public async Task<bool> SendShippingNotificationAsync(Order order, string trackingNumber = "")
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (!settings.EnableEmailNotifications || !settings.SendShippingNotificationEmails)
                {
                    _logger.LogInformation("Shipping notification emails are disabled");
                    return false;
                }

                var htmlContent = await _templateService.GenerateShippingNotification(order, trackingNumber, settings);
                var subject = $"Your Order Has Shipped - {order.OrderNumber}";

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    order.CustomerEmail,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Shipping notification sent via Resend for order {order.OrderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending shipping notification via Resend for order {order.OrderNumber}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(string email, string resetLink)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (!settings.EnableEmailNotifications)
                {
                    _logger.LogInformation("Email notifications are disabled");
                    return false;
                }

                var htmlContent = await _templateService.GeneratePasswordReset(resetLink, settings);
                var subject = "Password Reset Request";

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    email,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Password reset email sent via Resend to {email}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset via Resend to {email}");
                return false;
            }
        }

        public async Task<bool> SendEmailVerificationAsync(ApplicationUser user, string verificationLink)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (!settings.EnableEmailNotifications)
                {
                    _logger.LogInformation("Email notifications are disabled");
                    return false;
                }

                var htmlContent = await _templateService.GenerateEmailVerification(user, verificationLink, settings);
                var subject = $"Verify Your Email - {settings.SiteName}";

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    user.Email!,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Email verification sent via Resend to {user.Email}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email verification via Resend to {user.Email}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(ApplicationUser user)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (!settings.EnableEmailNotifications)
                {
                    _logger.LogInformation("Email notifications are disabled");
                    return false;
                }

                var htmlContent = await _templateService.GenerateWelcomeEmail(user, settings);
                var subject = $"Welcome to {settings.SiteName}!";

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    user.Email!,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Welcome email sent via Resend to {user.Email}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending welcome email via Resend to {user.Email}");
                return false;
            }
        }

        public async Task<bool> SendAdminOrderNotificationAsync(Order order)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (!settings.EnableEmailNotifications || !settings.SendAdminOrderNotifications)
                {
                    return false;
                }

                var adminEmail = settings.AdminNotificationEmail ?? settings.ContactEmail;
                var htmlContent = await _templateService.GenerateAdminNotification(order, settings);
                var subject = $"?? New Order Received - {order.OrderNumber}";

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    adminEmail,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Admin notification sent via Resend for order {order.OrderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending admin notification via Resend for order {order.OrderNumber}");
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                var htmlContent = await _templateService.GenerateTestEmail(settings);
                var subject = "Test Email from " + settings.SiteName;

                _logger.LogInformation($"Sending test email to {toEmail} from {settings.EmailFromAddress}");

                var result = await SendEmailViaResendAsync(
                    settings.EmailFromAddress,
                    toEmail,
                    subject,
                    htmlContent
                );

                if (result)
                {
                    _logger.LogInformation($"Test email sent via Resend to {toEmail}");
                }
                else
                {
                    _logger.LogWarning($"Test email failed to send to {toEmail}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending test email via Resend to {toEmail}");
                return false;
            }
        }

        /// <summary>
        /// Attaches logo for CID embedding if the HTML contains cid:logo reference
        /// </summary>
        private async Task AttachLogoIfNeeded(EmailMessage message, Models.SiteSettings settings, string htmlBody)
        {
            try
            {
                // Check if HTML contains cid:logo reference
                if (!htmlBody.Contains("cid:logo"))
                {
                    return;
                }

                // Get the logo image ID (prefer EmailLogoImageId, fallback to LogoImageId)
                Guid? logoImageId = settings.EmailLogoImageId ?? settings.LogoImageId;

                if (!logoImageId.HasValue || logoImageId.Value == Guid.Empty)
                {
                    _logger.LogWarning("Email contains cid:logo reference but no logo image ID is configured");
                    return;
                }

                // Retrieve the logo from the database
                var logoDataUri = await _storedImageService.GetImageAsBase64DataUriAsync(logoImageId.Value);

                if (string.IsNullOrEmpty(logoDataUri))
                {
                    _logger.LogWarning($"Failed to retrieve logo image with ID {logoImageId.Value}");
                    return;
                }

                // Parse the data URI to extract content type and base64 data
                // Expected format: data:image/svg+xml;base64,PHN2Zy...
                if (!logoDataUri.StartsWith("data:"))
                {
                    _logger.LogWarning("Logo data URI does not start with 'data:'");
                    return;
                }

                var dataUriParts = logoDataUri.Substring(5).Split(new[] { ';', ',' }, 3);
                if (dataUriParts.Length < 3)
                {
                    _logger.LogWarning("Invalid logo data URI format");
                    return;
                }

                var contentType = dataUriParts[0]; // e.g., "image/svg+xml"
                var base64Data = dataUriParts[2]; // Base64 string

                // Determine filename and normalize content type for email compatibility
                string filename;
                string normalizedContentType;
                
                if (contentType.Contains("svg"))
                {
                    filename = "logo.svg";
                    normalizedContentType = "image/svg+xml";
                }
                else if (contentType.Contains("png"))
                {
                    filename = "logo.png";
                    normalizedContentType = "image/png";
                }
                else if (contentType.Contains("jpeg") || contentType.Contains("jpg"))
                {
                    filename = "logo.jpg";
                    normalizedContentType = "image/jpeg";
                }
                else if (contentType.Contains("gif"))
                {
                    filename = "logo.gif";
                    normalizedContentType = "image/gif";
                }
                else
                {
                    // Default to PNG for unknown types
                    filename = "logo.png";
                    normalizedContentType = "image/png";
                }

                // Create attachment using Resend SDK - use base64 string directly
                // ContentId should match the cid: reference in HTML without extension
                message.Attachments = new List<Resend.EmailAttachment>
                {
                    new Resend.EmailAttachment
                    {
                        Filename = filename,
                        Content = base64Data,
                        ContentType = normalizedContentType, // Explicitly set correct MIME type
                        ContentId = "logo" // Matches cid:logo in HTML (without file extension)
                    }
                };

                _logger.LogDebug($"Added inline logo attachment: filename='{filename}', ContentId='logo', type={normalizedContentType}, size={base64Data.Length} chars)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating logo attachment for Resend API");
            }
        }
    }
}
