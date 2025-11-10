using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EcommerceStarter.Pages.Checkout
{
    public class CheckoutMethodModel : PageModel
    {
        private readonly ICartService _cartService;

        public CheckoutMethodModel(ICartService cartService)
        {
            _cartService = cartService;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }

        public IActionResult OnGet()
        {
            // If user is logged in, bypass method selection and go directly to checkout
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/Checkout/Index");
            }

            CartItems = _cartService.GetCart();
            Subtotal = _cartService.GetCartTotal();
            return Page();
        }
    }
}
