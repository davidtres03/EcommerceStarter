using System.ComponentModel.DataAnnotations;
using EcommerceStarter.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Orders
{
    public class TrackOrderModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TrackOrderModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Order Number")]
            public string OrderNumber { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Look up order by email AND order number for security
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => 
                    o.OrderNumber == Input.OrderNumber.ToUpper().Trim() && 
                    o.CustomerEmail.ToLower() == Input.Email.ToLower().Trim());

            if (order == null)
            {
                ErrorMessage = "No order found with that email address and order number combination. Please check your information and try again.";
                return Page();
            }

            // Redirect to order details page
            return RedirectToPage("/Orders/GuestOrderDetails", new { id = order.Id, email = Input.Email });
        }
    }
}
