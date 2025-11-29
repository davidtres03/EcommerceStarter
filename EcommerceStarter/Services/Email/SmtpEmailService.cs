// Harden SMTP configuration: read from ApiConfigurations (DB-only)
using System.Net;
using System.Net.Mail;
using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// SMTP email service implementation
    /// Works with Gmail, Outlook, or any SMTP server
    /// No additional packages required - uses built-in .NET System.Net.Mail
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IEncryptionService _encryptionService;
        private readonly IApiConfigurationService _apiConfigService;
        private readonly EmailTemplateService _templateService;
        private readonly IStoredImageService _storedImageService;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            ISiteSettingsService siteSettingsService,
            IEncryptionService encryptionService,
            IApiConfigurationService apiConfigurationService,
            EmailTemplateService templateService,
            IStoredImageService storedImageService,
            ILogger<SmtpEmailService> logger)
        {
            _siteSettingsService = siteSettingsService;
            _encryptionService = encryptionService;
            _apiConfigService = apiConfigurationService;
            _templateService = templateService;
            _storedImageService = storedImageService;
            _logger = logger;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            if (!settings.EnableEmailNotifications || settings.EmailProvider != EmailProvider.Smtp)
                return false;

            var smtpConfig = await GetSmtpConfigAsync();
            return smtpConfig != null &&
                   !string.IsNullOrEmpty(smtpConfig.Host) &&
                   !string.IsNullOrEmpty(smtpConfig.Username) &&
                   !string.IsNullOrEmpty(smtpConfig.Password);
        }

        private record SmtpConfig(string Host, int Port, bool UseSsl, string Username, string Password, string FromAddress, string FromName);

        private async Task<SmtpConfig?> GetSmtpConfigAsync()
        {
            try
            {
                var configs = await _apiConfigService.GetConfigurationsByTypeAsync("smtp");
                var cfg = configs.OrderBy(c => c.Id).FirstOrDefault();
                if (cfg == null)
                    return null;

                var values = await _apiConfigService.GetDecryptedValuesAsync(cfg.Id);
                var host = values.GetValueOrDefault("Value1");
                var portStr = values.GetValueOrDefault("Value2");
                var sslStr = values.GetValueOrDefault("Value3");
                var username = values.GetValueOrDefault("Value4");
                var password = values.GetValueOrDefault("Value5");
                var fromAddress = values.GetValueOrDefault("Value6") ?? (await _siteSettingsService.GetSettingsAsync()).EmailFromAddress;
                var fromName = values.GetValueOrDefault("Value7") ?? (await _siteSettingsService.GetSettingsAsync()).EmailFromName;

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                    return null;

                int port = 587;
                bool useSsl = true;
                int.TryParse(portStr, out port);
                if (bool.TryParse(sslStr, out var parsedSsl)) useSsl = parsedSsl;

                return new SmtpConfig(host, port, useSsl, username, password, fromAddress!, fromName!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read SMTP configuration from ApiConfigurations");
                return null;
            }
        }

        private async Task<SmtpClient?> CreateSmtpClientAsync()
        {
            try
            {
                var smtpConfig = await GetSmtpConfigAsync();
                if (smtpConfig == null)
                    return null;

                var smtpClient = new SmtpClient(smtpConfig.Host, smtpConfig.Port)
                {
                    EnableSsl = smtpConfig.UseSsl,
                    Credentials = new NetworkCredential(smtpConfig.Username, smtpConfig.Password),
                    Timeout = 30000
                };

                return smtpClient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create SMTP client");
                return null;
            }
        }

        private async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                var smtpClient = await CreateSmtpClientAsync();
                
                if (smtpClient == null)
                {
                    _logger.LogWarning("SMTP not configured");
                    return false;
                }

                using (smtpClient)
                {
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress((await GetSmtpConfigAsync())?.FromAddress ?? settings.EmailFromAddress, (await GetSmtpConfigAsync())?.FromName ?? settings.EmailFromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(to);

                    // Attach logo as embedded resource with CID if EmailLogoImageId exists
                    await AttachLogoIfNeeded(mailMessage, settings, htmlBody);

                    await smtpClient.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"SMTP error sending email to {to}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email via SMTP to {to}");
                return false;
            }
        }

        private async Task AttachLogoIfNeeded(MailMessage mailMessage, SiteSettings settings, string htmlBody)
        {
            // Only attach if the HTML contains the CID reference
            if (!htmlBody.Contains("cid:logo"))
                return;

            try
            {
                // Try EmailLogoImageId first, then LogoImageId
                Guid? imageId = settings.EmailLogoImageId ?? settings.LogoImageId;
                
                if (!imageId.HasValue || imageId.Value == Guid.Empty)
                    return;

                var image = await _storedImageService.GetImageAsync(imageId.Value);
                if (image == null || image.StorageType != "local")
                    return;

                var base64Data = await _storedImageService.GetDecryptedDataAsync(imageId.Value);
                if (string.IsNullOrEmpty(base64Data))
                    return;

                // Parse data URI: data:image/svg+xml;base64,{base64string}
                var parts = base64Data.Split(new[] { ";base64," }, StringSplitOptions.None);
                if (parts.Length != 2)
                    return;

                var contentType = parts[0].Replace("data:", "");
                var base64String = parts[1];
                var imageBytes = Convert.FromBase64String(base64String);

                using (var ms = new MemoryStream(imageBytes))
                {
                    var linkedResource = new LinkedResource(ms, contentType)
                    {
                        ContentId = "logo",
                        TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                    };

                    // Create alternate view for HTML with linked resource
                    var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                    htmlView.LinkedResources.Add(linkedResource);
                    
                    mailMessage.AlternateViews.Add(htmlView);
                    mailMessage.Body = ""; // Clear body since we're using AlternateView
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to attach logo as embedded resource, email will send without logo");
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

                var result = await SendEmailAsync(order.CustomerEmail, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Order confirmation email sent via SMTP for order {order.OrderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending order confirmation via SMTP for order {order.OrderNumber}");
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
                var subject = $"Your Order Has Shipped! - {order.OrderNumber}";

                var result = await SendEmailAsync(order.CustomerEmail, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Shipping notification sent via SMTP for order {order.OrderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending shipping notification via SMTP for order {order.OrderNumber}");
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

                var result = await SendEmailAsync(email, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Password reset email sent via SMTP to {email}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset via SMTP to {email}");
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

                var result = await SendEmailAsync(user.Email!, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Email verification sent via SMTP to {user.Email}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email verification via SMTP to {user.Email}");
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

                var result = await SendEmailAsync(user.Email!, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Welcome email sent via SMTP to {user.Email}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending welcome email via SMTP to {user.Email}");
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

                var result = await SendEmailAsync(adminEmail, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Admin notification sent via SMTP for order {order.OrderNumber}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending admin notification via SMTP for order {order.OrderNumber}");
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

                var result = await SendEmailAsync(toEmail, subject, htmlContent);
                
                if (result)
                {
                    _logger.LogInformation($"Test email sent via SMTP to {toEmail}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending test email via SMTP to {toEmail}");
                return false;
            }
        }
    }
}
