using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Cart
{
    public class CartModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _context;

        public CartModel(ICartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal Total { get; set; }
        public bool IsAuthenticated { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCartAsync();
        }

        public IActionResult OnPostUpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return OnPostRemoveItem(productId);
            }

            var cartItems = _cartService.GetCart();
            var cartItem = cartItems.FirstOrDefault(c => c.ProductId == productId);

            // Validate quantity doesn't exceed available stock
            if (cartItem != null && cartItem.StockQuantity > 0 && quantity > cartItem.StockQuantity)
            {
                quantity = cartItem.StockQuantity;
            }

            _cartService.UpdateQuantity(productId, quantity);
            SuccessMessage = "Cart updated!";
            return RedirectToPage();
        }

        public IActionResult OnPostRemoveItem(int productId)
        {
            _cartService.RemoveFromCart(productId);
            SuccessMessage = "Item removed from cart!";
            return RedirectToPage();
        }

        public IActionResult OnPostClearCart()
        {
            _cartService.ClearCart();
            SuccessMessage = "Cart cleared!";
            return RedirectToPage();
        }

        private async Task LoadCartAsync()
        {
            CartItems = _cartService.GetCart();
            
            // Refresh stock information from database for each cart item
            foreach (var item in CartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Update stock quantity from database
                    item.StockQuantity = product.StockQuantity;
                    
                    // Cap quantity if it exceeds current stock
                    if (product.StockQuantity > 0 && item.Quantity > product.StockQuantity)
                    {
                        item.Quantity = product.StockQuantity;
                        _cartService.UpdateQuantity(item.ProductId, item.Quantity);
                    }
                }
            }
            
            Total = _cartService.GetCartTotal();
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;
        }
    }
}
