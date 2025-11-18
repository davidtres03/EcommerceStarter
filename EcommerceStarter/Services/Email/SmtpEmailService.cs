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
        private readonly EmailTemplateService _templateService;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            ISiteSettingsService siteSettingsService,
            IEncryptionService encryptionService,
            EmailTemplateService templateService,
            ILogger<SmtpEmailService> logger)
        {
            _siteSettingsService = siteSettingsService;
            _encryptionService = encryptionService;
            _templateService = templateService;
            _logger = logger;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            return settings.EnableEmailNotifications && 
                   settings.EmailProvider == EmailProvider.Smtp && 
                   !string.IsNullOrEmpty(settings.SmtpHost) &&
                   !string.IsNullOrEmpty(settings.SmtpUsername) &&
                   !string.IsNullOrEmpty(settings.SmtpPassword);
        }

        private async Task<SmtpClient?> CreateSmtpClientAsync()
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();
                
                if (string.IsNullOrEmpty(settings.SmtpHost) || 
                    string.IsNullOrEmpty(settings.SmtpUsername) || 
                    string.IsNullOrEmpty(settings.SmtpPassword))
                {
                    return null;
                }

                string password;
                try
                {
                    // Try to decrypt - if it fails, assume it's already plaintext
                    password = _encryptionService.Decrypt(settings.SmtpPassword);
                }
                catch (Exception decryptEx)
                {
                    _logger.LogWarning(decryptEx, "Failed to decrypt SMTP password - assuming plaintext");
                    // If decryption fails, use the password as-is (likely plaintext)
                    password = settings.SmtpPassword;
                }

                var smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.SmtpUseSsl,
                    Credentials = new NetworkCredential(settings.SmtpUsername, password),
                    Timeout = 30000 // 30 seconds
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
                        From = new MailAddress(settings.EmailFromAddress, settings.EmailFromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(to);

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

                var htmlContent = _templateService.GenerateOrderConfirmation(order, settings);
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

                var htmlContent = _templateService.GenerateShippingNotification(order, trackingNumber, settings);
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

                var htmlContent = _templateService.GeneratePasswordReset(resetLink, settings);
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

                var htmlContent = _templateService.GenerateEmailVerification(user, verificationLink, settings);
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

                var htmlContent = _templateService.GenerateWelcomeEmail(user, settings);
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
                var htmlContent = _templateService.GenerateAdminNotification(order, settings);
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
                var htmlContent = _templateService.GenerateTestEmail(settings);
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
