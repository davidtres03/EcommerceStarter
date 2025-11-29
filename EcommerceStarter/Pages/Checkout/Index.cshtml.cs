using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Pages.Checkout
{
    public class CheckoutModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutModel> _logger;

        public CheckoutModel(
            ICartService cartService, 
            ApplicationDbContext context, 
            ILogger<CheckoutModel> logger)
        {
            _cartService = cartService;
            _context = context;
            _logger = logger;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public List<string> StockErrors { get; set; } = new();

        [BindProperty]
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Display(Name = "Street Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        public string ShippingCity { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be 2 characters (e.g., CA, NY)")]
        [Display(Name = "State (e.g., CA)")]
        public string ShippingState { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Display(Name = "ZIP Code")]
        public string ShippingZip { get; set; } = string.Empty;

        [BindProperty]
        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "card";

        public async Task<IActionResult> OnGetAsync(bool asGuest = false)
        {
            LoadCart();
            
            if (!CartItems.Any())
            {
                return RedirectToPage("/Cart/Index");
            }

            // Store guest checkout preference
            if (asGuest)
            {
                HttpContext.Session.SetString("IsGuestCheckout", "true");
            }

            // Pre-fill user information if logged in (not guest)
            if (!asGuest && User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        Email = user.Email ?? "";
                        PhoneNumber = user.PhoneNumber ?? "";
                    }
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadCart();

            _logger.LogInformation($"Checkout form submitted - Cart has {CartItems.Count} items, Subtotal: {Subtotal:C}");

            if (!CartItems.Any())
            {
                _logger.LogWarning("Cart empty during checkout");
                return RedirectToPage("/Cart/Index");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Model validation failed: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)))}");
                return Page();
            }

            // Validate stock availability
            var productIds = CartItems.Select(c => c.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Check stock for each item
            foreach (var cartItem in CartItems)
            {
                var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                
                if (product == null)
                {
                    StockErrors.Add($"{cartItem.ProductName} is no longer available.");
                    continue;
                }

                if (!product.IsAvailable)
                {
                    StockErrors.Add($"{product.Name} is currently out of stock.");
                    continue;
                }

                if (product.StockQuantity < cartItem.Quantity)
                {
                    StockErrors.Add($"{product.Name}: Only {product.StockQuantity} available (you have {cartItem.Quantity} in cart).");
                }
            }

            // If there are stock errors, return to page with errors
            if (StockErrors.Any())
            {
                _logger.LogWarning($"Stock validation failed: {string.Join(", ", StockErrors)}");
                ModelState.AddModelError(string.Empty, "Some items in your cart are no longer available or have insufficient stock.");
                return Page();
            }

            // Store checkout information in session
            HttpContext.Session.SetString("CheckoutFullName", FullName);
            HttpContext.Session.SetString("CheckoutEmail", Email);
            HttpContext.Session.SetString("CheckoutPhone", PhoneNumber);
            HttpContext.Session.SetString("CheckoutAddress", ShippingAddress);
            HttpContext.Session.SetString("CheckoutCity", ShippingCity);
            HttpContext.Session.SetString("CheckoutState", ShippingState.ToUpper());
            HttpContext.Session.SetString("CheckoutZip", ShippingZip);
            
            // Store selected payment method
            HttpContext.Session.SetString("CheckoutPaymentMethod", PaymentMethod);

            _logger.LogInformation(
                $"Checkout information saved to session:\n" +
                $"   Name: {FullName}\n" +
                $"   Email: {Email}\n" +
                $"   Address: {ShippingAddress}, {ShippingCity}, {ShippingState} {ShippingZip}\n" +
                $"   Total: {Subtotal:C}\n" +
                $"   Payment Method: {PaymentMethod}"
            );

            // Redirect directly to payment page
            _logger.LogInformation("Redirecting to Payment page");
            return RedirectToPage("/Checkout/Payment");
        }

        private void LoadCart()
        {
            CartItems = _cartService.GetCart();
            Subtotal = _cartService.GetCartTotal();
            Total = Subtotal;
        }
    }
}
