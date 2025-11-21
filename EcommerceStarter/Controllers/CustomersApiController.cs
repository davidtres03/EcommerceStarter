using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/customers")]
    public class CustomersApiController : ControllerBase
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomersApiController> _logger;

        public CustomersApiController(ApplicationDbContext context, ILogger<CustomersApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Users
                    .Include(u => u.Orders)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(u =>
                        (u.Email != null && u.Email.Contains(search)) ||
                        (u.UserName != null && u.UserName.Contains(search)));
                }

                var totalCount = await query.CountAsync();

                var customers = await query
                    .OrderBy(u => u.UserName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        Id = u.Id,
                        FirstName = u.UserName ?? "",
                        LastName = "",
                        Email = u.Email ?? "",
                        Phone = u.PhoneNumber ?? "",
                        Address = (string?)null,
                        City = (string?)null,
                        State = (string?)null,
                        ZipCode = (string?)null,
                        Country = (string?)null,
                        TotalOrders = u.Orders.Count,
                        TotalSpent = u.Orders.Sum(o => o.TotalAmount),
                        CreatedAt = u.CreatedAt.ToString(DateTimeFormat),
                        LastOrderDate = u.Orders.Any() ? u.Orders.Max(o => o.OrderDate).ToString(DateTimeFormat) : null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    customers,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customers");
                return StatusCode(500, new { message = "Error fetching customers" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(string id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { message = "Customer not found" });

                return Ok(new
                {
                    Id = user.Id,
                    FirstName = user.UserName ?? "",
                    LastName = "",
                    Email = user.Email ?? "",
                    Phone = user.PhoneNumber ?? "",
                    Address = (string?)null,
                    City = (string?)null,
                    State = (string?)null,
                    ZipCode = (string?)null,
                    Country = (string?)null,
                    TotalOrders = user.Orders.Count,
                    TotalSpent = user.Orders.Sum(o => o.TotalAmount),
                    CreatedAt = user.CreatedAt.ToString(DateTimeFormat),
                    LastOrderDate = user.Orders.Any() ? user.Orders.Max(o => o.OrderDate).ToString(DateTimeFormat) : null,
                    IsActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching customer {CustomerId}", id);
                return StatusCode(500, new { message = "Error fetching customer" });
            }
        }

        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivateCustomer(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                user.LockoutEnabled = false;
                user.LockoutEnd = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {CustomerId} activated", id);

                return Ok(new
                {
                    success = true,
                    message = "Customer activated successfully",
                    data = new { Id = user.Id, IsActive = true }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating customer {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error activating customer" });
            }
        }

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateCustomer(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {CustomerId} deactivated", id);

                return Ok(new
                {
                    success = true,
                    message = "Customer deactivated successfully",
                    data = new { Id = user.Id, IsActive = false }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating customer {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error deactivating customer" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                // Check if customer has orders
                if (user.Orders.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete customer with existing orders. Consider deactivating instead."
                    });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {CustomerId} deleted", id);

                return Ok(new
                {
                    success = true,
                    message = "Customer deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting customer" });
            }
        }

        [HttpPost("{id}/resend-verification")]
        public async Task<IActionResult> ResendVerificationEmail(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                if (user.EmailConfirmed)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Customer email is already verified"
                    });
                }

                // TODO: Implement email sending logic
                _logger.LogInformation("Verification email resend requested for customer {CustomerId}", id);

                return Ok(new
                {
                    success = true,
                    message = "Verification email sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email for customer {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error sending verification email" });
            }
        }

        [HttpGet("{id}/audit-log")]
        public async Task<IActionResult> GetCustomerAuditLog(
            string id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { success = false, message = "Customer not found" });

                // Get security audit events for this customer
                var query = _context.SecurityAuditLogs
                    .Where(log => log.UserEmail == user.Email || log.UserId == id);

                var totalCount = await query.CountAsync();

                var auditLogs = await query
                    .OrderByDescending(log => log.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(log => new
                    {
                        log.Id,
                        log.Timestamp,
                        log.EventType,
                        log.Severity,
                        log.IpAddress,
                        log.Details
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
                _logger.LogError(ex, "Error fetching audit log for customer {CustomerId}", id);
                return StatusCode(500, new { success = false, message = "Error fetching audit log" });
            }
        }
    }
}
