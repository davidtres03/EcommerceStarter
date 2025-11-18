using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Orders
{
    public class GuestOrderDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GuestOrderDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Orders/TrackOrder");
            }

            // Verify email matches order for security
            Order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => 
                    o.Id == id && 
                    o.CustomerEmail.ToLower() == email.ToLower());

            if (Order == null)
            {
                TempData["ErrorMessage"] = "Order not found or email doesn't match.";
                return RedirectToPage("/Orders/TrackOrder");
            }

            return Page();
        }
    }
}
