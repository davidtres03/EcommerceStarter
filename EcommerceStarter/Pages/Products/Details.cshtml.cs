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

        /// <summary>
        /// Gets the product image URL, using encrypted StoredImages if ProductImageId is set,
        /// otherwise falls back to legacy ImageUrl
        /// </summary>
        public string GetProductImageUrl()
        {
            if (Product?.ProductImageId.HasValue == true)
            {
                return $"/images/stored/{Product.ProductImageId.Value}";
            }
            return Product?.ImageUrl ?? "/images/placeholder.jpg";
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Load product with basic navigation properties
            Product = await _context.Products
                .Include(p => p.CategoryNavigation)
                .Include(p => p.SubCategoryNavigation)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (Product == null)
            {
                return Page();
            }

            // Load variants separately to avoid cartesian explosion
            Product.Variants = await _context.ProductVariants
                .Where(v => v.ProductId == id)
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();

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
                // Load variants with their attribute values and the attribute definitions
                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .Include(v => v.AttributeValues)
                        .ThenInclude(av => av.VariantAttribute)
                    .OrderBy(v => v.DisplayOrder)
                    .ToListAsync();

                // Build the result with properly structured data
                var result = variants.Select(variant =>
                {
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

                return new JsonResult(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
    }
}
