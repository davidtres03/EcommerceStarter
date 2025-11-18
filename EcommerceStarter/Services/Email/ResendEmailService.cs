using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EcommerceStarter.Models;

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
        private readonly ILogger<ResendEmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ResendApiUrl = "https://api.resend.com/emails";

        public ResendEmailService(
            ISiteSettingsService siteSettingsService,
            IEncryptionService encryptionService,
            EmailTemplateService templateService,
            ILogger<ResendEmailService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _siteSettingsService = siteSettingsService;
            _encryptionService = encryptionService;
            _templateService = templateService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            return settings.EnableEmailNotifications && 
                   settings.EmailProvider == EmailProvider.Resend && 
                   !string.IsNullOrEmpty(settings.ResendApiKey);
        }

        private async Task<string?> GetApiKeyAsync()
        {
            var settings = await _siteSettingsService.GetSettingsAsync();
            
            if (string.IsNullOrEmpty(settings.ResendApiKey))
            {
                return null;
            }

            try
            {
                // Try to decrypt - if it fails, assume it's already plaintext
                return _encryptionService.Decrypt(settings.ResendApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt Resend API key - assuming plaintext");
                // If decryption fails, return the key as-is (likely plaintext)
                return settings.ResendApiKey;
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

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var payload = new
                {
                    from,
                    to = new[] { to },
                    subject,
                    html = htmlBody
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync(ResendApiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Resend API error: {response.StatusCode} - {error}");
                    return false;
                }
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

                var htmlContent = _templateService.GenerateOrderConfirmation(order, settings);
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

                var htmlContent = _templateService.GenerateShippingNotification(order, trackingNumber, settings);
                var subject = $"Your Order Has Shipped! - {order.OrderNumber}";

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

                var htmlContent = _templateService.GeneratePasswordReset(resetLink, settings);
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

                var htmlContent = _templateService.GenerateEmailVerification(user, verificationLink, settings);
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

                var htmlContent = _templateService.GenerateWelcomeEmail(user, settings);
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
                var htmlContent = _templateService.GenerateAdminNotification(order, settings);
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
                var htmlContent = _templateService.GenerateTestEmail(settings);
                var subject = "Test Email from " + settings.SiteName;

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

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending test email via Resend to {toEmail}");
                return false;
            }
        }
    }
}
