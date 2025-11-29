using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Email service interface - provider-agnostic
    /// Implemented by Resend, Brevo, SMTP, and SendGrid services
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send order confirmation email to customer
        /// </summary>
        Task<bool> SendOrderConfirmationAsync(Order order);

        /// <summary>
        /// Send shipping notification email with optional tracking number
        /// </summary>
        Task<bool> SendShippingNotificationAsync(Order order, string trackingNumber = "");

        /// <summary>
        /// Send password reset email with reset link
        /// </summary>
        Task<bool> SendPasswordResetAsync(string email, string resetLink);

        /// <summary>
        /// Send email verification link to new user
        /// </summary>
        Task<bool> SendEmailVerificationAsync(ApplicationUser user, string verificationLink);

        /// <summary>
        /// Send welcome email to new user
        /// </summary>
        Task<bool> SendWelcomeEmailAsync(ApplicationUser user);

        /// <summary>
        /// Send order notification to admin
        /// </summary>
        Task<bool> SendAdminOrderNotificationAsync(Order order);

        /// <summary>
        /// Send test email to verify configuration
        /// </summary>
        Task<bool> SendTestEmailAsync(string toEmail);

        /// <summary>
        /// Check if email service is properly configured
        /// </summary>
        Task<bool> IsConfiguredAsync();
    }
}
