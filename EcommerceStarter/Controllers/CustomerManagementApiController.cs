using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/customers")]
    public class CustomerManagementApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<CustomerManagementApiController> _logger;

        public CustomerManagementApiController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<CustomerManagementApiController> logger)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // PUT: api/customers/{id}/activate
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateCustomer(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                // Check if user is already active (not locked out)
                if (user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow)
                {
                    return BadRequest(new { success = false, message = "Customer account is already active" });
                }

                // Remove lockout
                var result = await _userManager.SetLockoutEndDateAsync(user, null);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to activate customer {UserId}: {Errors}", user.Id, errors);
                    return StatusCode(500, new { success = false, message = "Failed to activate customer account", errors });
                }

                // Reset access failed count
                await _userManager.ResetAccessFailedCountAsync(user);

                // Log the action
                _context.CustomerAuditLogs.Add(new CustomerAuditLog
                {
                    CustomerId = user.Id,
                    EventType = "Account Activated",
                    Description = "Admin activated account",
                    Category = AuditEventCategory.AdminAction,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer account activated: {Email} (ID: {UserId})", user.Email, user.Id);

                return Ok(new
                {
                    success = true,
                    message = "Customer account activated successfully",
                    data = new
                    {
                        user.Id,
                        user.Email,
                        IsActive = true,
                        LockoutEnd = (DateTimeOffset?)null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating customer {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error activating customer account" });
            }
        }

        // PUT: api/customers/{id}/deactivate
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateCustomer(string id, [FromBody] DeactivateCustomerRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                // Check if user is an admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return BadRequest(new { success = false, message = "Cannot deactivate admin accounts through this endpoint" });
                }

                // Check if user is already deactivated
                if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    return BadRequest(new { success = false, message = "Customer account is already deactivated" });
                }

                // Set lockout end date to far future (permanent lockout)
                var lockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to deactivate customer {UserId}: {Errors}", user.Id, errors);
                    return StatusCode(500, new { success = false, message = "Failed to deactivate customer account", errors });
                }

                // Log the action
                _context.CustomerAuditLogs.Add(new CustomerAuditLog
                {
                    CustomerId = user.Id,
                    EventType = "Account Deactivated",
                    Description = $"Admin deactivated account. Reason: {request.Reason ?? "Not specified"}",
                    Category = AuditEventCategory.AdminAction,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer account deactivated: {Email} (ID: {UserId}). Reason: {Reason}",
                    user.Email, user.Id, request.Reason ?? "Not specified");

                return Ok(new
                {
                    success = true,
                    message = "Customer account deactivated successfully",
                    data = new
                    {
                        user.Id,
                        user.Email,
                        IsActive = false,
                        LockoutEnd = lockoutEnd
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating customer {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error deactivating customer account" });
            }
        }

        // DELETE: api/customers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                // Check if user is an admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    return BadRequest(new { success = false, message = "Cannot delete admin accounts through this endpoint" });
                }

                // Check if user has orders
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == id);
                if (hasOrders)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete customer with existing orders. Consider deactivating the account instead.",
                        hasOrders = true
                    });
                }

                // Log the deletion before deleting
                var email = user.Email;
                _logger.LogWarning("Deleting customer account: {Email} (ID: {UserId})", email, user.Id);

                // Delete the user
                var result = await _userManager.DeleteAsync(user);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to delete customer {UserId}: {Errors}", user.Id, errors);
                    return StatusCode(500, new { success = false, message = "Failed to delete customer account", errors });
                }

                _logger.LogInformation("Customer account deleted: {Email} (ID: {UserId})", email, id);

                return Ok(new
                {
                    success = true,
                    message = "Customer account deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting customer account" });
            }
        }

        // POST: api/customers/{id}/resend-verification
        [HttpPost("{id}/resend-verification")]
        public async Task<IActionResult> ResendVerificationEmail(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                // Check if email is already confirmed
                if (user.EmailConfirmed)
                {
                    return BadRequest(new { success = false, message = "Email is already verified" });
                }

                // Generate email confirmation token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Create verification link (this would be the mobile app deep link or web URL)
                var verificationLink = $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                // Send verification email
                var emailSent = await _emailService.SendEmailVerificationAsync(user, verificationLink);

                if (!emailSent)
                {
                    _logger.LogWarning("Failed to send verification email to {Email}", user.Email);
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Failed to send verification email. Email service may not be configured."
                    });
                }

                // Log the action
                _context.CustomerAuditLogs.Add(new CustomerAuditLog
                {
                    CustomerId = user.Id,
                    EventType = "Verification Email Resent",
                    Description = "Admin triggered verification email resend",
                    Category = AuditEventCategory.Email,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Verification email resent to {Email} (ID: {UserId})", user.Email, user.Id);

                return Ok(new
                {
                    success = true,
                    message = "Verification email sent successfully",
                    data = new
                    {
                        user.Id,
                        user.Email,
                        EmailSent = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email for customer {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error resending verification email" });
            }
        }

        // GET: api/customers/{id}/audit-log
        [HttpGet("{id}/audit-log")]
        public async Task<IActionResult> GetCustomerAuditLog(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Customer not found" });
                }

                var totalCount = await _context.CustomerAuditLogs
                    .Where(log => log.CustomerId == id)
                    .CountAsync();

                var auditLogs = await _context.CustomerAuditLogs
                    .Where(log => log.CustomerId == id)
                    .OrderByDescending(log => log.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(log => new
                    {
                        log.Id,
                        EventType = log.EventType,
                        Description = log.Description,
                        Category = log.Category.ToString(),
                        Timestamp = log.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = auditLogs,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit log for customer {UserId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving customer audit log" });
            }
        }
    }

    // Request DTOs
    public class DeactivateCustomerRequest
    {
        public string? Reason { get; set; }
    }
}
