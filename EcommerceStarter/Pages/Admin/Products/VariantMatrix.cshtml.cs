using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Pages.Admin.Products
{
    [Authorize(Roles = "Admin")]
    public class VariantMatrixModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VariantMatrixModel> _logger;

        public VariantMatrixModel(
            ApplicationDbContext context,
            ILogger<VariantMatrixModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Product? Product { get; set; }
        public List<VariantAttribute> Attributes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _context.Products.FindAsync(id);
            if (Product == null)
                return NotFound();

            // Load all variant attributes for this product
            Attributes = await _context.VariantAttributes
                .Where(va => va.ProductId == id)
                .OrderBy(va => va.DisplayOrder)
                .ToListAsync();

            return Page();
        }

        /// <summary>
        /// Get all values for selected attributes
        /// </summary>
        public async Task<IActionResult> OnGetGetAttributeValuesAsync(string attrIds, int productId)
        {
            try
            {
                if (string.IsNullOrEmpty(attrIds))
                    return new JsonResult(new { success = false, message = "No attributes selected" });

                var ids = attrIds.Split(',').Select(int.Parse).ToList();

                var attributes = await _context.VariantAttributes
                    .Where(va => va.ProductId == productId && ids.Contains(va.Id))
                    .ToListAsync();

                var result = new Dictionary<string, List<string>>();
                foreach (var attr in attributes)
                {
                    var values = attr.Values.Split(',')
                        .Select(v => v.Trim())
                        .Where(v => !string.IsNullOrEmpty(v))
                        .ToList();
                    result[attr.Id.ToString()] = values;
                }

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attribute values");
                return new JsonResult(new { success = false, message = "Error loading attribute values" });
            }
        }

        /// <summary>
        /// Create multiple variants from matrix selections
        /// </summary>
        public async Task<IActionResult> OnPostCreateVariantMatrixAsync([FromBody] CreateVariantMatrixRequest request)
        {
            try
            {
                if (request?.Variants == null || request.Variants.Count == 0)
                    return new JsonResult(new { success = false, message = "No variants to create" });

                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId);

                if (product == null)
                    return new JsonResult(new { success = false, message = "Product not found" });

                int variantsCreated = 0;
                var maxOrder = product.Variants?.Any() == true 
                    ? product.Variants.Max(v => v.DisplayOrder) 
                    : 0;

                foreach (var variantRequest in request.Variants)
                {
                    try
                    {
                        maxOrder++;

                        // Create the variant
                        var variant = new ProductVariant
                        {
                            ProductId = request.ProductId,
                            Name = variantRequest.Name,
                            StockQuantity = variantRequest.Stock,
                            DisplayOrder = maxOrder,
                            IsAvailable = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.ProductVariants.Add(variant);
                        await _context.SaveChangesAsync();

                        // Add attribute value mappings
                        if (variantRequest.Attributes != null && variantRequest.Attributes.Count > 0)
                        {
                            foreach (var attr in variantRequest.Attributes)
                            {
                                var attributeValue = new VariantAttributeValue
                                {
                                    ProductVariantId = variant.Id,
                                    VariantAttributeId = int.Parse(attr.AttributeId),
                                    Value = attr.Value,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.VariantAttributeValues.Add(attributeValue);
                            }

                            await _context.SaveChangesAsync();
                        }

                        variantsCreated++;
                        _logger.LogInformation(
                            "Variant created: ProductId={ProductId}, Name={VariantName}, Stock={Stock}",
                            request.ProductId, variant.Name, variant.StockQuantity);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating individual variant: {VariantName}", variantRequest.Name);
                    }
                }

                return new JsonResult(new 
                { 
                    success = true, 
                    message = $"{variantsCreated} variants created successfully",
                    variantsCreated 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating variant matrix");
                return new JsonResult(new { success = false, message = "Error creating variants" });
            }
        }

        /// <summary>
        /// Request model for creating variant matrix
        /// </summary>
        public class CreateVariantMatrixRequest
        {
            [Required]
            public int ProductId { get; set; }

            [Required]
            public List<VariantMatrixItem> Variants { get; set; } = new();
        }

        public class VariantMatrixItem
        {
            [Required]
            public string Name { get; set; } = string.Empty;

            [Range(0, 10000)]
            public int Stock { get; set; }

            public List<VariantAttributeMapping> Attributes { get; set; } = new();
        }

        public class VariantAttributeMapping
        {
            public string AttributeId { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }
    }
}

