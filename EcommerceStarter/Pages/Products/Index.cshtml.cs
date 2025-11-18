using Microsoft.AspNetCore.Mvc.RazorPages;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Services;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Products
{
    public class ProductsModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _context;

        public ProductsModel(ICartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        public List<Product> Products { get; private set; } = new();
        public string? CurrentCategory { get; private set; }
        public string? CurrentSubCategory { get; private set; }
        
        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync(string? category = null, string? subcategory = null)
        {
            CurrentCategory = category;
            CurrentSubCategory = subcategory;

            var query = _context.Products
                .Include(p => p.Variants)
                .AsQueryable();

            // Filter products based on variant availability
            // Products with variants: show if ANY variant is in stock
            // Products without variants: use legacy status (InStock or ComingSoon)
            var allProducts = await query.ToListAsync();
            
            var availableProducts = allProducts.Where(p => 
            {
                // Hide Out of Stock products
                if (p.InventoryStatus == InventoryStatus.OutOfStock)
                    return false;

                // If product has variants, check if any variant is in stock
                if (p.Variants != null && p.Variants.Count > 0)
                {
                    return p.Variants.Any(v => v.IsInStock);
                }
                
                // Otherwise, use legacy status (show InStock or ComingSoon, hide OutOfStock)
                return p.InventoryStatus == InventoryStatus.InStock || p.InventoryStatus == InventoryStatus.ComingSoon;
            });

            var productsForCategory = availableProducts.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                productsForCategory = productsForCategory.Where(p => p.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(subcategory))
            {
                productsForCategory = productsForCategory.Where(p => p.SubCategory.ToLower() == subcategory.ToLower());
            }

            Products = productsForCategory.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            
            // Prevent adding Coming Soon or Out of Stock products to cart
            if (product == null || !product.IsAvailable || product.IsComingSoon)
            {
                return RedirectToPage();
            }

            if (quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
            }

            var cartItem = new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl,
                StockQuantity = product.StockQuantity
            };

            _cartService.AddToCart(cartItem);
            
            SuccessMessage = $"{product.Name} added to cart!";
            
            return RedirectToPage(new { category = CurrentCategory, subcategory = CurrentSubCategory });
        }
    }
}
