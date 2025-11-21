using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EcommerceStarter.Models;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/account")]
    public class AccountApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountApiController> _logger;

        public AccountApiController(
            UserManager<ApplicationUser> userManager,
            ILogger<AccountApiController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPut("email")]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Verify current password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Invalid password during email update attempt for user {UserId}", userId);
                return BadRequest(new { success = false, message = "Current password is incorrect" });
            }

            // Check if new email is already in use
            var existingUser = await _userManager.FindByEmailAsync(request.NewEmail);
            if (existingUser != null && existingUser.Id != userId)
            {
                return BadRequest(new { success = false, message = "Email address is already in use" });
            }

            // Update email
            var token = await _userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail);
            var result = await _userManager.ChangeEmailAsync(user, request.NewEmail, token);

            if (result.Succeeded)
            {
                // Also update username to match email
                user.UserName = request.NewEmail;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Email updated successfully for user {UserId}", userId);
                return Ok(new { success = true, message = "Email updated successfully" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update email for user {UserId}: {Errors}", userId, errors);
            return BadRequest(new { success = false, message = $"Failed to update email: {errors}" });
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            // Verify new password matches confirmation
            if (request.NewPassword != request.ConfirmPassword)
            {
                return BadRequest(new { success = false, message = "New password and confirmation do not match" });
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", userId);
                return Ok(new { success = true, message = "Password changed successfully" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to change password for user {UserId}: {Errors}", userId, errors);
            return BadRequest(new { success = false, message = $"Failed to change password: {errors}" });
        }
    }

    public class UpdateEmailRequest
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = "";

        [Required]
        public string CurrentPassword { get; set; } = "";
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string NewPassword { get; set; } = "";

        [Required]
        public string ConfirmPassword { get; set; } = "";
    }
}
