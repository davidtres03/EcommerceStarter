using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        public List<ApplicationUser> Customers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var allUsers = await _userManager.Users
                .Include(u => u.Orders)
                .ToListAsync();
            
            Customers = new List<ApplicationUser>();
            foreach (var user in allUsers)
            {
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    Customers.Add(user);
                }
            }
        }

        public async Task<IActionResult> OnPostDeactivateAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                // Lock the user account (cannot sign in)
                user.LockoutEnd = DateTimeOffset.MaxValue;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} (ID: {userId}) has been deactivated by admin");
                    TempData["Success"] = $"User '{user.Email}' has been deactivated successfully.";
                }
                else
                {
                    _logger.LogWarning($"Failed to deactivate user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    TempData["Error"] = "Failed to deactivate user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating user {userId}");
                TempData["Error"] = "An error occurred while deactivating the user.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostActivateAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                // Unlock the user account
                user.LockoutEnd = null;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} (ID: {userId}) has been activated by admin");
                    TempData["Success"] = $"User '{user.Email}' has been activated successfully.";
                }
                else
                {
                    _logger.LogWarning($"Failed to activate user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    TempData["Error"] = "Failed to activate user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating user {userId}");
                TempData["Error"] = "An error occurred while activating the user.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                // Log user details before deletion
                var userEmail = user.Email;
                var userOrders = await _userManager.Users
                    .Where(u => u.Id == userId)
                    .Include(u => u.Orders)
                    .Select(u => u.Orders.Count)
                    .FirstOrDefaultAsync();

                // Hard delete the user (this will cascade delete associated Identity data)
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {userEmail} (ID: {userId}) has been permanently deleted by admin. Orders count: {userOrders}");
                    TempData["Success"] = $"User '{userEmail}' and all associated data have been permanently deleted.";
                }
                else
                {
                    _logger.LogWarning($"Failed to delete user {userEmail}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    TempData["Error"] = $"Failed to delete user. {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {userId}");
                TempData["Error"] = "An error occurred while deleting the user.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResendEmailVerificationAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                // Check if email is already confirmed
                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    TempData["Error"] = $"User '{user.Email}' has already confirmed their email.";
                    return RedirectToPage();
                }

                // Generate new confirmation token
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Create confirmation link
                var confirmationLink = Url.PageLink(
                    pageName: "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = user.Id, code = confirmationToken },
                    protocol: Request.Scheme);

                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(user, confirmationLink ?? string.Empty);
                
                if (emailSent)
                {
                    _logger.LogInformation($"Email verification resent to {user.Email} by admin (ID: {userId})");
                    TempData["Success"] = $"Email verification resent to {user.Email}. They have 24 hours to confirm.";
                }
                else
                {
                    _logger.LogWarning($"Failed to resend verification email to {user.Email}");
                    TempData["Error"] = "Failed to send verification email. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending email verification to user {userId}");
                TempData["Error"] = "An error occurred while sending the email verification.";
            }

            return RedirectToPage();
        }
    }
}
