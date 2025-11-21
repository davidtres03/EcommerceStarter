using EcommerceStarter.Data;
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
    public class DetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly IEmailService _emailService;
        private readonly IAuditLogService _auditLogService;

        public DetailsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<DetailsModel> logger,
            IEmailService emailService,
            IAuditLogService auditLogService)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _auditLogService = auditLogService;
        }

        public ApplicationUser? Customer { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItems { get; set; }
        public bool IsDeactivated { get; set; }
        public List<CustomerAuditLog> AuditLogs { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            Customer = await _userManager.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (Customer == null)
            {
                return Page();
            }

            // Check if user is a customer
            if (!await _userManager.IsInRoleAsync(Customer, "Customer"))
            {
                return RedirectToPage("./Index");
            }

            // Check if user is deactivated
            IsDeactivated = Customer.LockoutEnd.HasValue && Customer.LockoutEnd > DateTime.UtcNow;

            // Calculate statistics
            TotalOrders = Customer.Orders.Count;
            TotalSpent = Customer.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .Sum(o => o.TotalAmount);
            AverageOrderValue = TotalOrders > 0 ? TotalSpent / TotalOrders : 0;
            TotalItems = Customer.Orders
                .Where(o => o.Status != OrderStatus.Cancelled)
                .SelectMany(o => o.OrderItems)
                .Sum(oi => oi.Quantity);

            // Load audit logs (last 90 days)
            AuditLogs = await _auditLogService.GetCustomerLogsAsync(id, days: 90);

            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage(new { id });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} (ID: {id}) has been deactivated by admin from details page");
                    
                    // Log admin action
                    var adminEmail = User.Identity?.Name ?? "Unknown Admin";
                    await _auditLogService.LogAdminActionAsync(id, adminEmail, "Deactivate", "Account deactivated by administrator");
                    
                    TempData["Success"] = "User has been deactivated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to deactivate user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deactivating user {id}");
                TempData["Error"] = "An error occurred while deactivating the user.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostActivateAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage(new { id });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                user.LockoutEnd = null;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.Email} (ID: {id}) has been activated by admin from details page");
                    
                    // Log admin action
                    var adminEmail = User.Identity?.Name ?? "Unknown Admin";
                    await _auditLogService.LogAdminActionAsync(id, adminEmail, "Activate", "Account activated by administrator");
                    
                    TempData["Success"] = "User has been activated successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to activate user.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error activating user {id}");
                TempData["Error"] = "An error occurred while activating the user.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            try
            {
                var userEmail = user.Email;
                
                // Log admin action before deletion
                var adminEmail = User.Identity?.Name ?? "Unknown Admin";
                await _auditLogService.LogAccountDeletedAsync(id, adminEmail);
                
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {userEmail} (ID: {id}) has been permanently deleted by admin from details page");
                    TempData["Success"] = $"User '{userEmail}' and all associated data have been permanently deleted.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    TempData["Error"] = $"Failed to delete user. {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                TempData["Error"] = "An error occurred while deleting the user.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostResendEmailConfirmationAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid user ID.";
                return RedirectToPage(new { id });
            }

            var user = await _userManager.FindByIdAsync(id);
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
                    TempData["Error"] = "This user's email is already confirmed.";
                    return RedirectToPage(new { id });
                }

                // Generate new confirmation token
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                
                // Create confirmation link
                var confirmationLink = Url.PageLink(
                    pageName: "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = user.Id, code = confirmationToken },
                    protocol: Request.Scheme);

                // Send verification email (will be logged by AuditedEmailService)
                var emailSent = await _emailService.SendEmailVerificationAsync(user, confirmationLink ?? string.Empty);
                
                if (emailSent)
                {
                    _logger.LogInformation($"Email verification resent to {user.Email} by admin (ID: {id})");
                    
                    // Log admin action
                    var adminEmail = User.Identity?.Name ?? "Unknown Admin";
                    await _auditLogService.LogAdminActionAsync(id, adminEmail, "ResendVerification", "Email verification resent by administrator");
                    
                    TempData["Success"] = $"Email verification resent to {user.Email}. User has 24 hours to confirm their email.";
                }
                else
                {
                    _logger.LogWarning($"Failed to resend verification email to {user.Email}");
                    TempData["Error"] = "Failed to send verification email. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending email confirmation to user {id}");
                TempData["Error"] = "An error occurred while sending the email verification.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUpdateCustomerAsync(
            string id,
            string? phoneNumber,
            string? address,
            string? city,
            string? state,
            string? postalCode)
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Invalid user ID.";
                return RedirectToPage(new { id });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return RedirectToPage();
            }

            try
            {
                // Track changes for audit log
                var changes = new List<string>();

                // Update phone number
                if (user.PhoneNumber != phoneNumber)
                {
                    var oldPhone = user.PhoneNumber ?? "None";
                    user.PhoneNumber = phoneNumber;
                    changes.Add($"Phone: {oldPhone} → {phoneNumber ?? "None"}");
                }

                // Update address fields
                if (user.Address != address)
                {
                    var oldAddress = user.Address ?? "None";
                    user.Address = address;
                    changes.Add($"Address: {oldAddress} → {address ?? "None"}");
                }

                if (user.City != city)
                {
                    var oldCity = user.City ?? "None";
                    user.City = city;
                    changes.Add($"City: {oldCity} → {city ?? "None"}");
                }

                if (user.State != state)
                {
                    var oldState = user.State ?? "None";
                    user.State = state;
                    changes.Add($"State: {oldState} → {state ?? "None"}");
                }

                if (user.PostalCode != postalCode)
                {
                    var oldPostalCode = user.PostalCode ?? "None";
                    user.PostalCode = postalCode;
                    changes.Add($"Postal Code: {oldPostalCode} → {postalCode ?? "None"}");
                }

                if (changes.Any())
                {
                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation(
                            "Customer {UserId} ({Email}) updated by admin. Changes: {Changes}",
                            id,
                            user.Email,
                            string.Join(", ", changes)
                        );

                        // Log admin action
                        var adminEmail = User.Identity?.Name ?? "Unknown Admin";
                        await _auditLogService.LogAdminActionAsync(
                            id,
                            adminEmail,
                            "UpdateInfo",
                            $"Customer information updated: {string.Join(", ", changes)}"
                        );

                        SuccessMessage = "Customer information updated successfully.";
                    }
                    else
                    {
                        ErrorMessage = $"Failed to update customer: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                    }
                }
                else
                {
                    ErrorMessage = "No changes detected.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {UserId}", id);
                ErrorMessage = "An error occurred while updating customer information.";
            }

            return RedirectToPage(new { id });
        }
    }
}
