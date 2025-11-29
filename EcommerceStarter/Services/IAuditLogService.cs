using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for logging customer activities and events
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Log a successful login event
        /// </summary>
        Task LogLoginAsync(string customerId, string ipAddress, string userAgent);

        /// <summary>
        /// Log a failed login attempt
        /// </summary>
        Task LogFailedLoginAsync(string email, string ipAddress, string userAgent, string reason);

        /// <summary>
        /// Log a logout event
        /// </summary>
        Task LogLogoutAsync(string customerId, string ipAddress, string userAgent);

        /// <summary>
        /// Log an email sent event
        /// </summary>
        Task LogEmailSentAsync(string customerId, string emailType, string subject, bool success, string? errorMessage = null);

        /// <summary>
        /// Log a password reset request
        /// </summary>
        Task LogPasswordResetRequestAsync(string customerId, string ipAddress, string userAgent);

        /// <summary>
        /// Log a password change
        /// </summary>
        Task LogPasswordChangeAsync(string customerId, string ipAddress, string userAgent, bool success);

        /// <summary>
        /// Log an email verification event
        /// </summary>
        Task LogEmailVerificationAsync(string customerId, string ipAddress, string userAgent, bool success);

        /// <summary>
        /// Log an email address change
        /// </summary>
        Task LogEmailChangeAsync(string customerId, string oldEmail, string newEmail, string ipAddress, string userAgent);

        /// <summary>
        /// Log an admin action on a customer account
        /// </summary>
        Task LogAdminActionAsync(string customerId, string adminEmail, string action, string details);

        /// <summary>
        /// Log account creation
        /// </summary>
        Task LogAccountCreatedAsync(string customerId, string ipAddress, string userAgent);

        /// <summary>
        /// Log account deletion
        /// </summary>
        Task LogAccountDeletedAsync(string customerId, string adminEmail);

        /// <summary>
        /// Get audit logs for a specific customer with optional filtering
        /// </summary>
        Task<List<CustomerAuditLog>> GetCustomerLogsAsync(string customerId, AuditEventCategory? category = null, int days = 90);

        /// <summary>
        /// Clean up old audit logs (older than 90 days, except orders which are kept for 3 years)
        /// </summary>
        Task CleanupOldLogsAsync();
    }
}
