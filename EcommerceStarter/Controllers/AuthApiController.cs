using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EcommerceStarter.Models;
using EcommerceStarter.Services.Auth;
using EcommerceStarter.Services;
using EcommerceStarter.Extensions;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            ISecurityAuditService securityAuditService,
            ISecuritySettingsService securitySettingsService,
            IAuditLogService auditLogService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _securityAuditService = securityAuditService;
            _securitySettingsService = securitySettingsService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var ipAddress = HttpContext.GetClientIpAddress();
            var userAgent = HttpContext.GetUserAgent();
            var settings = await _securitySettingsService.GetSettingsAsync();

            // Check if IP is whitelisted
            var isWhitelisted = await _securitySettingsService.IsIpWhitelistedAsync(ipAddress);

            if (!isWhitelisted && settings.EnableIpBlocking)
            {
                // Check for excessive failed login attempts
                var failedAttempts = await _securityAuditService.GetFailedLoginAttemptsAsync(
                    ipAddress,
                    settings.FailedLoginWindowMinutes);

                if (failedAttempts >= settings.MaxFailedLoginAttempts)
                {
                    await _securityAuditService.BlockIpAsync(
                        ipAddress,
                        $"Exceeded {settings.MaxFailedLoginAttempts} failed login attempts",
                        settings.IpBlockDurationMinutes);

                    await _securityAuditService.LogSecurityEventAsync(
                        "MobileLoginBlockedExcessiveAttempts",
                        "Critical",
                        ipAddress,
                        userEmail: request.Email,
                        details: $"Mobile login blocked after {failedAttempts} failed attempts",
                        endpoint: "/api/auth/login",
                        userAgent: userAgent,
                        isBlocked: true);

                    return StatusCode(429, new LoginResponse
                    {
                        Success = false,
                        Message = "Too many failed login attempts. Your IP has been temporarily blocked."
                    });
                }
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Mobile login attempt for non-existent user {Email}", request.Email);
                await LogFailedLoginAttempt(request.Email, ipAddress, userAgent, "User not found");
                return Unauthorized(new LoginResponse { Success = false, Message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: settings.EnableAccountLockout);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                // Only allow Admin role for mobile app
                if (role != "Admin")
                {
                    _logger.LogWarning("Non-admin user {Email} attempted mobile login", request.Email);
                    await LogFailedLoginAttempt(request.Email, ipAddress, userAgent, "Non-admin user");
                    return Unauthorized(new LoginResponse { Success = false, Message = "Access denied. Admin role required." });
                }

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, role);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Store refresh token in database
                await _refreshTokenService.CreateRefreshTokenAsync(user.Id, refreshToken, ipAddress, userAgent);

                // Log successful login
                _logger.LogInformation("Mobile login successful for user {Email} from IP {IpAddress}", request.Email, ipAddress);
                await _auditLogService.LogLoginAsync(user.Id, ipAddress, userAgent);

                if (settings.EnableSecurityAuditLogging)
                {
                    await _securityAuditService.LogSecurityEventAsync(
                        "MobileLoginSuccess",
                        "Low",
                        ipAddress,
                        userEmail: request.Email,
                        details: "Mobile login successful",
                        endpoint: "/api/auth/login",
                        userAgent: userAgent);
                }

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600, // 1 hour
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        Name = user.UserName ?? user.Email!,
                        Role = role,
                        AvatarUrl = null
                    },
                    Message = "Login successful"
                });
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Mobile login attempt on locked account {Email}", request.Email);
                await LogFailedLoginAttempt(request.Email, ipAddress, userAgent, "Account locked");
                return StatusCode(423, new LoginResponse { Success = false, Message = "Account is locked out. Please try again later." });
            }
            else
            {
                _logger.LogWarning("Invalid mobile login attempt for {Email}", request.Email);
                await LogFailedLoginAttempt(request.Email, ipAddress, userAgent, "Invalid credentials");
                return Unauthorized(new LoginResponse { Success = false, Message = "Invalid email or password" });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var ipAddress = HttpContext.GetClientIpAddress();

            // Validate access token (even expired, just to get claims)
            var principal = _jwtService.ValidateToken(request.AccessToken);
            if (principal == null)
            {
                _logger.LogWarning("Invalid access token in refresh request from IP {IpAddress}", ipAddress);
                return Unauthorized(new { success = false, message = "Invalid access token" });
            }

            var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { success = false, message = "Invalid token claims" });
            }

            // Validate refresh token
            var isValid = await _refreshTokenService.ValidateRefreshTokenAsync(request.RefreshToken, userId);
            if (!isValid)
            {
                _logger.LogWarning("Invalid refresh token for user {UserId} from IP {IpAddress}", userId, ipAddress);
                return Unauthorized(new { success = false, message = "Invalid refresh token" });
            }

            // Get user and generate new tokens
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, role);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Revoke old refresh token and create new one
            await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);
            await _refreshTokenService.CreateRefreshTokenAsync(user.Id, newRefreshToken, ipAddress, Request.Headers["User-Agent"].ToString());

            _logger.LogInformation("Token refreshed for user {UserId} from IP {IpAddress}", userId, ipAddress);

            return Ok(new LoginResponse
            {
                Success = true,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 3600,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Name = user.UserName ?? user.Email!,
                    Role = role,
                    AvatarUrl = null
                },
                Message = "Token refreshed successfully"
            });
        }

        [HttpPost("logout")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Revoke refresh token
            await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);

            _logger.LogInformation("User {UserId} logged out from mobile app", userId);

            return Ok(new { success = true, message = "Logged out successfully" });
        }

        [HttpPost("revoke-all")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> RevokeAllTokens()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            await _refreshTokenService.RevokeAllUserTokensAsync(userId);

            _logger.LogInformation("All tokens revoked for user {UserId}", userId);

            return Ok(new { success = true, message = "All refresh tokens revoked successfully" });
        }

        private async Task LogFailedLoginAttempt(string email, string ipAddress, string userAgent, string reason)
        {
            await _auditLogService.LogFailedLoginAsync(email, ipAddress, userAgent, reason);

            var settings = await _securitySettingsService.GetSettingsAsync();
            if (settings.EnableSecurityAuditLogging)
            {
                var failedAttempts = await _securityAuditService.GetFailedLoginAttemptsAsync(
                    ipAddress,
                    settings.FailedLoginWindowMinutes);

                await _securityAuditService.LogSecurityEventAsync(
                    "MobileLoginFailed",
                    "Medium",
                    ipAddress,
                    userEmail: email,
                    details: $"{reason}. Failed attempts from this IP: {failedAttempts + 1}",
                    endpoint: "/api/auth/login",
                    userAgent: userAgent);
            }
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

        public DeviceInfo? DeviceInfo { get; set; }
    }

    public class DeviceInfo
    {
        public string DeviceId { get; set; } = "";
        public string Os { get; set; } = "Android";
        public string OsVersion { get; set; } = "";
        public string AppVersion { get; set; } = "";
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public int? ExpiresIn { get; set; }
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

    public class RefreshTokenRequest
    {
        [Required]
        public string AccessToken { get; set; } = "";

        [Required]
        public string RefreshToken { get; set; } = "";
    }

    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = "";
    }
}
