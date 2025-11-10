using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    public class RegisterConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterConfirmationModel> _logger;

        public RegisterConfirmationModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<RegisterConfirmationModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostResendEmailAsync(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Please enter an email address.";
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal if user exists for security
                TempData["Message"] = "If an account with that email exists, we've resent the verification email.";
                return Page();
            }

            try
            {
                // Check if email is already confirmed
                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    TempData["Message"] = "Your email is already verified. You can now sign in.";
                    return RedirectToPage("Login");
                }

                // Generate new confirmation token
                var newToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Create confirmation link
                var confirmationLink = Url.PageLink(
                    pageName: "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = user.Id, code = newToken },
                    protocol: Request.Scheme);

                if (string.IsNullOrEmpty(confirmationLink))
                {
                    _logger.LogError($"Failed to generate confirmation link for {user.Email}");
                    TempData["Error"] = "Failed to generate confirmation link. Please try again later.";
                    return Page();
                }

                // Send verification email
                var sent = await _emailService.SendEmailVerificationAsync(user, confirmationLink);
                
                if (sent)
                {
                    _logger.LogInformation($"Resent verification email to {user.Email}");
                    TempData["Message"] = "Verification email has been resent. Please check your inbox.";
                }
                else
                {
                    _logger.LogWarning($"Failed to resend verification email to {user.Email}");
                    TempData["Error"] = "Failed to send verification email. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending verification email to {email}");
                TempData["Error"] = "An error occurred while sending the verification email.";
            }

            return Page();
        }
    }
}
