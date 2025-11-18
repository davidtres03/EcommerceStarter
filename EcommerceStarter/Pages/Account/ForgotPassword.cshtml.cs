using System.ComponentModel.DataAnnotations;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IAuditLogService auditLogService,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var ipAddress = HttpContext.GetClientIpAddress();
            var userAgent = HttpContext.GetUserAgent();
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                
                // For security, we don't reveal whether the user exists
                // Always show the same success message
                if (user == null)
                {
                    _logger.LogWarning($"Password reset requested for non-existent email: {Input.Email}");
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Check if email is confirmed
                if (!await _userManager.IsEmailConfirmedAsync(user))
                {
                    _logger.LogWarning($"Password reset requested for unconfirmed email: {Input.Email}");
                    // For security, don't reveal that email is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Log password reset request to customer audit log
                await _auditLogService.LogPasswordResetRequestAsync(user.Id, ipAddress, userAgent);

                // Generate password reset token
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                
                // Create reset link
                var resetLink = Url.PageLink(
                    pageName: "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { code },
                    protocol: Request.Scheme);

                if (string.IsNullOrEmpty(resetLink))
                {
                    _logger.LogError($"Failed to generate reset link for {user.Email}");
                    // For security, still show success message
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Send password reset email
                // Note: Password reset emails are NOT logged to customer audit (only the request is logged)
                // to avoid duplicate entries since we already logged the request above
                await _emailService.SendPasswordResetAsync(Input.Email, resetLink);

                _logger.LogInformation($"Password reset email sent to {Input.Email}");

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
