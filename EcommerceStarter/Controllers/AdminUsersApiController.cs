using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUsersApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminUsersApiController> _logger;

        public AdminUsersApiController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminUsersApiController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: api/admin/users
        [HttpGet]
        public async Task<IActionResult> GetAdminUsers()
        {
            try
            {
                // Get all users with Admin role
                var adminRole = await _roleManager.FindByNameAsync("Admin");
                if (adminRole == null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new List<object>(),
                        count = 0,
                        message = "Admin role not found"
                    });
                }

                var usersInRole = await _userManager.GetUsersInRoleAsync("Admin");

                var adminUsers = usersInRole.Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.EmailConfirmed,
                    user.PhoneNumber,
                    user.CreatedAt,
                    IsCurrentUser = user.Id == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                }).OrderBy(u => u.Email).ToList();

                return Ok(new
                {
                    success = true,
                    data = adminUsers,
                    count = adminUsers.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin users");
                return StatusCode(500, new { success = false, message = "Error retrieving admin users" });
            }
        }

        // POST: api/admin/users
        [HttpPost]
        public async Task<IActionResult> AddAdminUser([FromBody] AddAdminUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email is required" });
                }

                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found with this email address" });
                }

                // Check if user is already an admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return BadRequest(new { success = false, message = "User is already an admin" });
                }

                // Ensure Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        return StatusCode(500, new { success = false, message = "Failed to create Admin role" });
                    }
                }

                // Add user to Admin role
                var result = await _userManager.AddToRoleAsync(user, "Admin");

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to add user {UserId} to Admin role: {Errors}", user.Id, errors);
                    return StatusCode(500, new { success = false, message = "Failed to add admin privileges", errors });
                }

                _logger.LogInformation("User {Email} (ID: {UserId}) granted admin privileges", user.Email, user.Id);

                return Ok(new
                {
                    success = true,
                    message = "Admin privileges granted successfully",
                    data = new
                    {
                        user.Id,
                        user.Email,
                        user.UserName,
                        user.EmailConfirmed,
                        user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding admin user");
                return StatusCode(500, new { success = false, message = "Error adding admin user" });
            }
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveAdminUser(string id)
        {
            try
            {
                // Prevent removing yourself as admin
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (id == currentUserId)
                {
                    return BadRequest(new { success = false, message = "You cannot remove your own admin privileges" });
                }

                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Check if user is an admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    return BadRequest(new { success = false, message = "User is not an admin" });
                }

                // Remove user from Admin role
                var result = await _userManager.RemoveFromRoleAsync(user, "Admin");

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to remove user {UserId} from Admin role: {Errors}", user.Id, errors);
                    return StatusCode(500, new { success = false, message = "Failed to remove admin privileges", errors });
                }

                _logger.LogInformation("Admin privileges removed from user {Email} (ID: {UserId})", user.Email, user.Id);

                return Ok(new
                {
                    success = true,
                    message = "Admin privileges removed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing admin user {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error removing admin user" });
            }
        }

        // GET: api/admin/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAdminUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        user.Id,
                        user.Email,
                        user.UserName,
                        user.EmailConfirmed,
                        user.PhoneNumber,
                        user.CreatedAt,
                        IsAdmin = isAdmin,
                        IsCurrentUser = user.Id == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving user" });
            }
        }
    }

    // Request DTOs
    public class AddAdminUserRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
