using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Implementation of audit logging service for customer activities
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(ApplicationDbContext context, ILogger<AuditLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogLoginAsync(string customerId, string ipAddress, string userAgent)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "Login",
                Category = AuditEventCategory.Authentication,
                Description = "User logged in successfully",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = true
            });
        }

        public async Task LogFailedLoginAsync(string email, string ipAddress, string userAgent, string reason)
        {
            // Try to find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Don't log failed attempts for non-existent users (security)
                return;
            }

            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = user.Id,
                EventType = "FailedLogin",
                Category = AuditEventCategory.Authentication,
                Description = "Failed login attempt",
                Details = JsonSerializer.Serialize(new { Reason = reason }),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = false,
                ErrorMessage = reason
            });
        }

        public async Task LogLogoutAsync(string customerId, string ipAddress, string userAgent)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "Logout",
                Category = AuditEventCategory.Authentication,
                Description = "User logged out",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = true
            });
        }

        public async Task LogEmailSentAsync(string customerId, string emailType, string subject, bool success, string? errorMessage = null)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = $"Email{emailType}",
                Category = AuditEventCategory.Email,
                Description = $"{emailType} email {(success ? "sent" : "failed")}",
                Details = JsonSerializer.Serialize(new { Subject = subject, EmailType = emailType }),
                Success = success,
                ErrorMessage = errorMessage
            });
        }

        public async Task LogPasswordResetRequestAsync(string customerId, string ipAddress, string userAgent)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "PasswordResetRequest",
                Category = AuditEventCategory.Security,
                Description = "Password reset requested",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = true
            });
        }

        public async Task LogPasswordChangeAsync(string customerId, string ipAddress, string userAgent, bool success)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "PasswordChange",
                Category = AuditEventCategory.Account,
                Description = success ? "Password changed successfully" : "Password change failed",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success
            });
        }

        public async Task LogEmailVerificationAsync(string customerId, string ipAddress, string userAgent, bool success)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "EmailVerification",
                Category = AuditEventCategory.Security,
                Description = success ? "Email verified successfully" : "Email verification failed",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = success
            });
        }

        public async Task LogEmailChangeAsync(string customerId, string oldEmail, string newEmail, string ipAddress, string userAgent)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "EmailChange",
                Category = AuditEventCategory.Account,
                Description = "Email address changed",
                Details = JsonSerializer.Serialize(new { OldEmail = oldEmail, NewEmail = newEmail }),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = true
            });
        }

        public async Task LogAdminActionAsync(string customerId, string adminEmail, string action, string details)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = $"Admin{action}",
                Category = AuditEventCategory.AdminAction,
                Description = $"Admin action: {action}",
                Details = JsonSerializer.Serialize(new { AdminEmail = adminEmail, Action = action, Details = details }),
                Success = true
            });
        }

        public async Task LogAccountCreatedAsync(string customerId, string ipAddress, string userAgent)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "AccountCreated",
                Category = AuditEventCategory.Account,
                Description = "Account created",
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Success = true
            });
        }

        public async Task LogAccountDeletedAsync(string customerId, string adminEmail)
        {
            await CreateLogEntryAsync(new CustomerAuditLog
            {
                CustomerId = customerId,
                EventType = "AccountDeleted",
                Category = AuditEventCategory.AdminAction,
                Description = "Account deleted by admin",
                Details = JsonSerializer.Serialize(new { AdminEmail = adminEmail }),
                Success = true
            });
        }

        public async Task<List<CustomerAuditLog>> GetCustomerLogsAsync(string customerId, AuditEventCategory? category = null, int days = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var query = _context.CustomerAuditLogs
                .Where(log => log.CustomerId == customerId && log.CreatedAt >= cutoffDate);

            if (category.HasValue)
            {
                query = query.Where(log => log.Category == category.Value);
            }

            return await query
                .OrderByDescending(log => log.CreatedAt)
                .ToListAsync();
        }

        public async Task CleanupOldLogsAsync()
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-90);

                // Delete logs older than 90 days
                var oldLogs = await _context.CustomerAuditLogs
                    .Where(log => log.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    _context.CustomerAuditLogs.RemoveRange(oldLogs);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Cleaned up {oldLogs.Count} old audit log entries");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old audit logs");
            }
        }

        private async Task CreateLogEntryAsync(CustomerAuditLog logEntry)
        {
            try
            {
                _context.CustomerAuditLogs.Add(logEntry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Don't throw - logging should never break the application
                _logger.LogError(ex, $"Failed to create audit log entry for customer {logEntry.CustomerId}");
            }
        }
    }
}
