using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public DetailsModel(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public Product? Product { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Console.WriteLine($"[OnGet] Loading product {id}...");
            
            // Load product with basic navigation properties
            Product = await _context.Products
                .Include(p => p.CategoryNavigation)
                .Include(p => p.SubCategoryNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Product == null)
            {
                Console.WriteLine($"[OnGet] Product {id} not found!");
                return Page();
            }

            // Load variants separately to avoid cartesian explosion
            Product.Variants = await _context.ProductVariants
                .Where(v => v.ProductId == id)
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();

            Console.WriteLine($"[OnGet] Product loaded: {Product.Name}");
            Console.WriteLine($"[OnGet] HasVariants flag: {Product.HasVariants}");
            Console.WriteLine($"[OnGet] Variants collection is null: {Product.Variants == null}");
            Console.WriteLine($"[OnGet] Variants count: {Product.Variants?.Count ?? 0}");
            
            if (Product.Variants != null && Product.Variants.Count > 0)
            {
                foreach (var v in Product.Variants)
                {
                    Console.WriteLine($"  [OnGet] Variant: {v.Name}, Stock: {v.StockQuantity}");
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int id, int quantity = 1, int variantId = 0)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null || !product.IsAvailable || product.IsComingSoon)
            {
                return RedirectToPage(new { id });
            }

            // If product has variants, we need to validate the selected variant
            if (product.Variants != null && product.Variants.Count > 0)
            {
                if (variantId == 0)
                {
                    // No variant selected
                    SuccessMessage = "Please select a variant first";
                    return RedirectToPage(new { id });
                }

                var selectedVariant = product.Variants.FirstOrDefault(v => v.Id == variantId);
                if (selectedVariant == null || !selectedVariant.IsInStock)
                {
                    return RedirectToPage(new { id });
                }

                // Check if requested quantity exceeds available stock
                if (quantity > selectedVariant.StockQuantity)
                {
                    quantity = selectedVariant.StockQuantity;
                }

                // Use variant-specific price and information
                var cartItem = new CartItem
                {
                    ProductId = product.Id,
                    VariantId = variantId,
                    ProductName = $"{product.Name} - {selectedVariant.Name}",
                    Price = selectedVariant.EffectivePrice,
                    Quantity = quantity,
                    ImageUrl = selectedVariant.ImageUrl ?? product.ImageUrl
                };

                _cartService.AddToCart(cartItem);
                SuccessMessage = $"{product.Name} ({selectedVariant.Name}) (x{quantity}) added to cart!";
            }
            else
            {
                // Legacy: product without variants
                if (quantity > product.TotalAvailableStock)
                {
                    quantity = product.TotalAvailableStock;
                }

                var cartItem = new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                };

                _cartService.AddToCart(cartItem);
                SuccessMessage = $"{product.Name} (x{quantity}) added to cart!";
            }

            return RedirectToPage(new { id });
        }

        /// <summary>
        /// AJAX handler to get variant data for dynamic filtering
        /// Returns all variants with their attributes for client-side filtering
        /// </summary>
        public async Task<IActionResult> OnGetVariantDataAsync(int productId)
        {
            try
            {
                Console.WriteLine($"[VariantData] Loading variants for product {productId}...");
                
                // Load variants with their attribute values and the attribute definitions
                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .Include(v => v.AttributeValues)
                        .ThenInclude(av => av.VariantAttribute)
                    .OrderBy(v => v.DisplayOrder)
                    .ToListAsync();

                Console.WriteLine($"[VariantData] Found {variants.Count} variants for product {productId}");

                // Build the result with properly structured data
                var result = variants.Select(variant =>
                {
                    Console.WriteLine($"  Variant: {variant.Name}, Stock: {variant.StockQuantity}, IsAvailable: {variant.IsAvailable}, IsInStock: {variant.IsInStock}, AttributeValues: {variant.AttributeValues.Count}");
                    
                    var attributes = variant.AttributeValues
                        .Where(av => av.VariantAttribute != null) // Ensure attribute is loaded
                        .OrderBy(av => av.VariantAttribute!.DisplayOrder)
                        .Select(av => new
                        {
                            attributeId = av.VariantAttributeId,
                            attributeName = av.VariantAttribute!.Name,
                            value = av.Value
                        })
                        .ToList();
                    
                    Console.WriteLine($"    Attributes: {string.Join(", ", attributes.Select(a => $"{a.attributeName}={a.value}"))}");

                    return new
                    {
                        id = variant.Id,
                        name = variant.Name,
                        stock = variant.StockQuantity,
                        isAvailable = variant.IsInStock,
                        imageUrl = variant.ImageUrl,
                        priceOverride = variant.PriceOverride,
                        attributes = attributes
                    };
                }).ToList();

                Console.WriteLine($"[VariantData] Returning {result.Count} variants with attributes");
                return new JsonResult(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VariantData] ERROR: {ex.Message}");
                Console.WriteLine($"[VariantData] Stack trace: {ex.StackTrace}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}
