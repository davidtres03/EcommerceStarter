using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Decorator for IEmailService that adds audit logging for all email operations
    /// </summary>
    public class AuditedEmailService : IEmailService
    {
        private readonly IEmailService _innerService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditedEmailService> _logger;

        public AuditedEmailService(
            IEmailService innerService,
            IAuditLogService auditLogService,
            ILogger<AuditedEmailService> logger)
        {
            _innerService = innerService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<bool> SendOrderConfirmationAsync(Order order)
        {
            var result = await _innerService.SendOrderConfirmationAsync(order);
            
            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _auditLogService.LogEmailSentAsync(
                    order.UserId,
                    "OrderConfirmation",
                    $"Order Confirmation - Order #{order.Id}",
                    result,
                    result ? null : "Failed to send order confirmation email"
                );
            }

            return result;
        }

        public async Task<bool> SendShippingNotificationAsync(Order order, string trackingNumber = "")
        {
            var result = await _innerService.SendShippingNotificationAsync(order, trackingNumber);
            
            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _auditLogService.LogEmailSentAsync(
                    order.UserId,
                    "ShippingNotification",
                    $"Shipping Notification - Order #{order.Id}",
                    result,
                    result ? null : "Failed to send shipping notification email"
                );
            }

            return result;
        }

        public async Task<bool> SendPasswordResetAsync(string email, string resetLink)
        {
            var result = await _innerService.SendPasswordResetAsync(email, resetLink);
            
            // Note: We don't log password reset emails in audit log here because
            // the password reset request is logged separately in the password reset handler
            // This prevents duplicate logging
            
            return result;
        }

        public async Task<bool> SendEmailVerificationAsync(ApplicationUser user, string verificationLink)
        {
            var result = await _innerService.SendEmailVerificationAsync(user, verificationLink);
            
            await _auditLogService.LogEmailSentAsync(
                user.Id,
                "EmailVerification",
                "Verify Your Email Address",
                result,
                result ? null : "Failed to send email verification"
            );

            return result;
        }

        public async Task<bool> SendWelcomeEmailAsync(ApplicationUser user)
        {
            var result = await _innerService.SendWelcomeEmailAsync(user);
            
            await _auditLogService.LogEmailSentAsync(
                user.Id,
                "Welcome",
                "Welcome to MyStore Supply Co.",
                result,
                result ? null : "Failed to send welcome email"
            );

            return result;
        }

        public async Task<bool> SendAdminOrderNotificationAsync(Order order)
        {
            // Admin notifications are not logged in customer audit logs
            return await _innerService.SendAdminOrderNotificationAsync(order);
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            // Test emails are not logged in customer audit logs
            return await _innerService.SendTestEmailAsync(toEmail);
        }

        public async Task<bool> IsConfiguredAsync()
        {
            return await _innerService.IsConfiguredAsync();
        }
    }
}
