using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Models;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AuthController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Invalid request data" 
                });
            }

            var result = await _signInManager.PasswordSignInAsync(
                request.Email, 
                request.Password, 
                request.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                
                if (user == null)
                {
                    return BadRequest(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "User not found" 
                    });
                }

                // Generate a simple token (in production, use JWT)
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        Name = user.FirstName + " " + user.LastName,
                        Role = "Admin",
                        AvatarUrl = null
                    },
                    Message = "Login successful"
                });
            }

            if (result.IsLockedOut)
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Account locked. Please try again later." 
                });
            }

            return Unauthorized(new LoginResponse 
            { 
                Success = false, 
                Message = "Invalid email or password" 
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public UserDto? User { get; set; }
        public string? Message { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string? AvatarUrl { get; set; }
    }
}
