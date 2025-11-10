using System.ComponentModel.DataAnnotations;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SettingsModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public ChangeEmailInputModel EmailInput { get; set; } = new();

        [BindProperty]
        public ChangePasswordInputModel PasswordInput { get; set; } = new();

        public class ChangeEmailInputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New Email")]
            public string NewEmail { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string Password { get; set; } = string.Empty;
        }

        public class ChangePasswordInputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string OldPassword { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            Email = user.Email ?? string.Empty;
            Username = user.UserName ?? string.Empty;
            CreatedAt = user.CreatedAt;

            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                LoadUserData(user);
                return Page();
            }

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, EmailInput.Password);
            if (!passwordCheck)
            {
                ModelState.AddModelError("EmailInput.Password", "Incorrect password.");
                LoadUserData(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (EmailInput.NewEmail != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, EmailInput.NewEmail);
                if (!setEmailResult.Succeeded)
                {
                    StatusMessage = "Error: Failed to update email.";
                    LoadUserData(user);
                    return Page();
                }

                // Also update username to match email
                await _userManager.SetUserNameAsync(user, EmailInput.NewEmail);

                // Refresh sign in to reflect the new email
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Your email has been updated successfully.";
            }
            else
            {
                StatusMessage = "Your email is unchanged.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                LoadUserData(user);
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user, 
                PasswordInput.OldPassword, 
                PasswordInput.NewPassword);

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                LoadUserData(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your password has been changed successfully.";

            return RedirectToPage();
        }

        private void LoadUserData(ApplicationUser user)
        {
            Email = user.Email ?? string.Empty;
            Username = user.UserName ?? string.Empty;
            CreatedAt = user.CreatedAt;
        }
    }
}
