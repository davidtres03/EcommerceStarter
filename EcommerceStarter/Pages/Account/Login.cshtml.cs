using System.ComponentModel.DataAnnotations;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ISecuritySettingsService _securitySettingsService;
        private readonly IAuditLogService _auditLogService;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            ISecurityAuditService securityAuditService,
            ISecuritySettingsService securitySettingsService,
            IAuditLogService auditLogService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _securityAuditService = securityAuditService;
            _securitySettingsService = securitySettingsService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            var ipAddress = HttpContext.GetClientIpAddress();
            var userAgent = HttpContext.GetUserAgent();
            var settings = await _securitySettingsService.GetSettingsAsync();

            if (ModelState.IsValid)
            {
                // Check if IP is whitelisted (bypass all security checks)
                var isWhitelisted = await _securitySettingsService.IsIpWhitelistedAsync(ipAddress);
                
                if (!isWhitelisted && settings.EnableIpBlocking)
                {
                    // Check for excessive failed login attempts from this IP
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
                            "LoginBlockedExcessiveAttempts",
                            "Critical",
                            ipAddress,
                            userEmail: Input.Email,
                            details: $"Login blocked after {failedAttempts} failed attempts",
                            endpoint: "/Account/Login",
                            userAgent: userAgent,
                            isBlocked: true);

                        ModelState.AddModelError(string.Empty, 
                            "Too many failed login attempts. Your IP has been temporarily blocked. Please try again later.");
                        return Page();
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email, 
                    Input.Password, 
                    Input.RememberMe, 
                    lockoutOnFailure: settings.EnableAccountLockout);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully from IP {IpAddress}", Input.Email, ipAddress);
                    
                    // Log to customer audit log
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        await _auditLogService.LogLoginAsync(user.Id, ipAddress, userAgent);
                    }
                    
                    if (settings.EnableSecurityAuditLogging)
                    {
                        await _securityAuditService.LogSecurityEventAsync(
                            "SuccessfulLogin",
                            "Low",
                            ipAddress,
                            userEmail: Input.Email,
                            details: "User logged in successfully",
                            endpoint: "/Account/Login",
                            userAgent: userAgent);
                    }

                    return LocalRedirect(returnUrl);
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account {Email} is locked out", Input.Email);
                    
                    if (settings.EnableSecurityAuditLogging)
                    {
                        await _securityAuditService.LogSecurityEventAsync(
                            "LoginLockedOut",
                            "High",
                            ipAddress,
                            userEmail: Input.Email,
                            details: "Login attempt on locked account",
                            endpoint: "/Account/Login",
                            userAgent: userAgent);
                    }

                    ModelState.AddModelError(string.Empty, "This account has been locked out. Please try again later.");
                    return Page();
                }
                else
                {
                    _logger.LogWarning("Invalid login attempt for {Email} from IP {IpAddress}", Input.Email, ipAddress);
                    
                    // Log failed login to customer audit log
                    await _auditLogService.LogFailedLoginAsync(Input.Email, ipAddress, userAgent, "Invalid credentials");
                    
                    if (settings.EnableSecurityAuditLogging && !isWhitelisted)
                    {
                        var failedAttempts = await _securityAuditService.GetFailedLoginAttemptsAsync(
                            ipAddress, 
                            settings.FailedLoginWindowMinutes);

                        await _securityAuditService.LogSecurityEventAsync(
                            "FailedLogin",
                            "Medium",
                            ipAddress,
                            userEmail: Input.Email,
                            details: $"Invalid credentials. Failed attempts from this IP: {failedAttempts + 1}",
                            endpoint: "/Account/Login",
                            userAgent: userAgent);
                    }

                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
