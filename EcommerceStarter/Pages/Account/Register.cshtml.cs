using System.ComponentModel.DataAnnotations;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            ISiteSettingsService siteSettingsService,
            IAuditLogService auditLogService,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _siteSettingsService = siteSettingsService;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            var ipAddress = HttpContext.GetClientIpAddress();
            var userAgent = HttpContext.GetUserAgent();
            
            if (ModelState.IsValid)
            {
                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                
                if (existingUser != null)
                {
                    // Email already exists
                    if (existingUser.EmailConfirmed)
                    {
                        // Email is confirmed - cannot register with this email
                        ModelState.AddModelError(string.Empty, 
                            "This email address is already registered and active. Please use a different email or sign in to your existing account.");
                        _logger.LogInformation($"Registration attempt with existing confirmed email: {Input.Email}");
                    }
                    else
                    {
                        // Email exists but not confirmed - offer to resend confirmation
                        ModelState.AddModelError(string.Empty, 
                            "This email was registered but never confirmed. We can resend the verification email. Please go to the login page and use the 'Didn't receive confirmation email?' option.");
                        _logger.LogInformation($"Registration attempt with existing unconfirmed email: {Input.Email}");
                    }
                    
                    return Page();
                }

                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Log account creation to customer audit log
                    await _auditLogService.LogAccountCreatedAsync(user.Id, ipAddress, userAgent);

                    // Assign Customer role to new users
                    await _userManager.AddToRoleAsync(user, "Customer");

                    // Generate email confirmation token
                    var emailConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    
                    // Create confirmation link
                    var confirmationLink = Url.PageLink(
                        pageName: "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { userId = user.Id, code = emailConfirmationToken },
                        protocol: Request.Scheme);

                    if (string.IsNullOrEmpty(confirmationLink))
                    {
                        _logger.LogError($"Failed to generate confirmation link for {user.Email}");
                        ModelState.AddModelError(string.Empty, "Failed to generate confirmation link. Please try again.");
                        await _userManager.DeleteAsync(user);
                        return Page();
                    }

                    try
                    {
                        // Send confirmation email (will be logged by AuditedEmailService)
                        var emailSent = await _emailService.SendEmailVerificationAsync(user, confirmationLink);
                        
                        if (emailSent)
                        {
                            _logger.LogInformation($"Email verification sent to {user.Email}");
                            
                            // Redirect to email confirmation page
                            return RedirectToPage("RegisterConfirmation");
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to send verification email to {user.Email}");
                            ModelState.AddModelError(string.Empty, "Failed to send verification email. Please try again.");
                            
                            // Delete the user since we couldn't send verification email
                            await _userManager.DeleteAsync(user);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending verification email to {user.Email}");
                        ModelState.AddModelError(string.Empty, "An error occurred while sending the verification email.");
                        
                        // Delete the user since we couldn't send verification email
                        await _userManager.DeleteAsync(user);
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            return Page();
        }
    }
}
