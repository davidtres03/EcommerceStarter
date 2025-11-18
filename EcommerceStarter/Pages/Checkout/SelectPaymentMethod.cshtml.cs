using System.Security.Claims;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Checkout
{
    // Removed [Authorize] to allow guest checkout
    public class SelectPaymentMethodModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ILogger<SelectPaymentMethodModel> _logger;

        public SelectPaymentMethodModel(
            ICartService cartService,
            ILogger<SelectPaymentMethodModel> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }

        // Checkout information from session
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingCity { get; set; } = string.Empty;
        public string ShippingState { get; set; } = string.Empty;
        public string ShippingZip { get; set; } = string.Empty;

        public IActionResult OnGet()
        {
            LoadCart();

            if (!CartItems.Any())
            {
                return RedirectToPage("/Cart/Index");
            }

            // Validate that checkout information exists in session
            Email = HttpContext.Session.GetString("CheckoutEmail") ?? "";
            PhoneNumber = HttpContext.Session.GetString("CheckoutPhone") ?? "";
            ShippingAddress = HttpContext.Session.GetString("CheckoutAddress") ?? "";
            ShippingCity = HttpContext.Session.GetString("CheckoutCity") ?? "";
            ShippingState = HttpContext.Session.GetString("CheckoutState") ?? "";
            ShippingZip = HttpContext.Session.GetString("CheckoutZip") ?? "";
            var taxString = HttpContext.Session.GetString("CheckoutTax") ?? "0";

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(ShippingAddress))
            {
                // Redirect back to checkout if information is missing
                return RedirectToPage("/Checkout/Index");
            }

            // Parse tax amount
            decimal.TryParse(taxString, out var taxAmountTemp);
            TaxAmount = taxAmountTemp;
            Total = Subtotal + TaxAmount;

            return Page();
        }

        public IActionResult OnPostSelectMethod(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
            {
                TempData["ErrorMessage"] = "Please select a payment method.";
                return RedirectToPage();
            }

            // Store selected payment method in session
            HttpContext.Session.SetString("SelectedPaymentMethod", paymentMethod);

            _logger.LogInformation($"Customer selected payment method: {paymentMethod}");

            // Redirect to payment page WITH selected method in query string
            return RedirectToPage("/Checkout/Payment", new { method = paymentMethod });
        }

        private void LoadCart()
        {
            CartItems = _cartService.GetCart();
            Subtotal = _cartService.GetCartTotal();
        }
    }
}
