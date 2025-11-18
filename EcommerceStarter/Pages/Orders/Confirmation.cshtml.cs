using System.Security.Claims;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Orders
{
    public class ConfirmationModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ConfirmationModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get order by ID, checking if it belongs to the user OR if user is guest
            Order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (Order == null)
            {
                return NotFound();
            }

            // Security check: Verify the order belongs to this user (or is accessible to guest)
            // For logged-in users: must match UserId
            // For guests: must have null UserId and match the order just created in this session
            if (!string.IsNullOrEmpty(userId))
            {
                // Logged-in user: verify ownership
                if (Order.UserId != userId)
                {
                    return NotFound();
                }
            }
            else
            {
                // Guest user: verify this is a guest order
                // Additional security: could check session token or recent creation time
                if (!string.IsNullOrEmpty(Order.UserId))
                {
                    // This order belongs to a registered user, guest can't access it
                    return NotFound();
                }
                
                // For guest orders, verify it was recently created (within last 10 minutes)
                // This prevents guests from accessing old guest orders
                if ((DateTime.UtcNow - Order.OrderDate).TotalMinutes > 10)
                {
                    // Order too old, redirect to track order page
                    return RedirectToPage("/Orders/TrackOrder");
                }
            }

            return Page();
        }
    }
}
