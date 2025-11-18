using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ConfirmEmailModel> _logger;

        public ConfirmEmailModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuditLogService auditLogService,
            ILogger<ConfirmEmailModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public string StatusMessage { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            var ipAddress = HttpContext.GetClientIpAddress();
            var userAgent = HttpContext.GetUserAgent();
            
            if (userId == null || code == null)
            {
                StatusMessage = "Invalid email confirmation link. Please try again or request a new confirmation email.";
                IsSuccess = false;
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                StatusMessage = "User not found. The confirmation link may be invalid or expired.";
                IsSuccess = false;
                return Page();
            }

            // Check if email is already confirmed
            if (user.EmailConfirmed)
            {
                StatusMessage = "Your email has already been confirmed. You can sign in to your account.";
                IsSuccess = true;
                return Page();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);
            
            if (result.Succeeded)
            {
                _logger.LogInformation($"Email confirmed for user {user.Email}");
                
                // Log email verification to customer audit log
                await _auditLogService.LogEmailVerificationAsync(user.Id, ipAddress, userAgent, success: true);
                
                // Send welcome email (will be logged by AuditedEmailService)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send welcome email to {user.Email}");
                    // Don't fail the confirmation if welcome email fails
                }
                
                StatusMessage = "Thank you for confirming your email! Your account is now active and you can sign in.";
                IsSuccess = true;
            }
            else
            {
                _logger.LogWarning($"Email confirmation failed for user {user.Email}");
                
                // Log failed email verification
                await _auditLogService.LogEmailVerificationAsync(user.Id, ipAddress, userAgent, success: false);
                
                StatusMessage = "Error confirming your email. The link may have expired or is invalid. Please request a new confirmation email.";
                IsSuccess = false;
            }

            return Page();
        }
    }
}
