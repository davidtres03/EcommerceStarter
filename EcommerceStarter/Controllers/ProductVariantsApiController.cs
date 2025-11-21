using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace EcommerceStarter.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    [ApiController]
    [Route("api/admin/products/{productId}/variants")]
    public class ProductVariantsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductVariantsApiController> _logger;

        public ProductVariantsApiController(ApplicationDbContext context, ILogger<ProductVariantsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/admin/products/{productId}/variants
        [HttpGet]
        public async Task<IActionResult> GetVariants(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return NotFound(new { message = "Product not found" });

                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .OrderBy(v => v.DisplayOrder)
                    .ThenBy(v => v.Name)
                    .Select(v => new
                    {
                        v.Id,
                        v.ProductId,
                        v.Name,
                        v.Sku,
                        v.StockQuantity,
                        v.ImageUrl,
                        v.PriceOverride,
                        v.IsAvailable,
                        v.IsFeatured,
                        v.DisplayOrder,
                        v.CreatedAt,
                        v.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { variants, totalCount = variants.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching variants for product {ProductId}", productId);
                return StatusCode(500, new { message = "Error fetching variants" });
            }
        }

        // GET: api/admin/products/{productId}/variants/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVariant(int productId, int id)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Where(v => v.Id == id && v.ProductId == productId)
                    .Select(v => new
                    {
                        v.Id,
                        v.ProductId,
                        v.Name,
                        v.Sku,
                        v.StockQuantity,
                        v.ImageUrl,
                        v.PriceOverride,
                        v.IsAvailable,
                        v.IsFeatured,
                        v.DisplayOrder,
                        v.CreatedAt,
                        v.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (variant == null)
                    return NotFound(new { message = "Variant not found" });

                return Ok(variant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching variant {VariantId} for product {ProductId}", id, productId);
                return StatusCode(500, new { message = "Error fetching variant" });
            }
        }

        // POST: api/admin/products/{productId}/variants
        [HttpPost]
        public async Task<IActionResult> CreateVariant(int productId, [FromBody] ProductVariantRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return NotFound(new { message = "Product not found" });

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Variant name is required" });

                // If featured, unfeatured other variants for this product
                if (request.IsFeatured)
                {
                    var existingFeatured = await _context.ProductVariants
                        .Where(v => v.ProductId == productId && v.IsFeatured)
                        .ToListAsync();
                    existingFeatured.ForEach(v => v.IsFeatured = false);
                }

                var variant = new ProductVariant
                {
                    ProductId = productId,
                    Name = request.Name,
                    Sku = request.Sku,
                    StockQuantity = request.StockQuantity,
                    ImageUrl = request.ImageUrl,
                    PriceOverride = request.PriceOverride,
                    IsAvailable = request.IsAvailable,
                    IsFeatured = request.IsFeatured,
                    DisplayOrder = request.DisplayOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProductVariants.Add(variant);

                // Update product HasVariants flag
                if (!product.HasVariants)
                {
                    product.HasVariants = true;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created variant {VariantName} for product {ProductId}", variant.Name, productId);

                return CreatedAtAction(
                    nameof(GetVariant),
                    new { productId, id = variant.Id },
                    new
                    {
                        variant.Id,
                        variant.ProductId,
                        variant.Name,
                        variant.Sku,
                        variant.StockQuantity,
                        variant.ImageUrl,
                        variant.PriceOverride,
                        variant.IsAvailable,
                        variant.IsFeatured,
                        variant.DisplayOrder,
                        variant.CreatedAt,
                        variant.UpdatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating variant for product {ProductId}", productId);
                return StatusCode(500, new { message = "Error creating variant" });
            }
        }

        // PUT: api/admin/products/{productId}/variants/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVariant(int productId, int id, [FromBody] ProductVariantRequest request)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == id && v.ProductId == productId);

                if (variant == null)
                    return NotFound(new { message = "Variant not found" });

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { message = "Variant name is required" });

                // If setting this variant as featured, unfeatured others
                if (request.IsFeatured && !variant.IsFeatured)
                {
                    var existingFeatured = await _context.ProductVariants
                        .Where(v => v.ProductId == productId && v.IsFeatured && v.Id != id)
                        .ToListAsync();
                    existingFeatured.ForEach(v => v.IsFeatured = false);
                }

                variant.Name = request.Name;
                variant.Sku = request.Sku;
                variant.StockQuantity = request.StockQuantity;
                variant.ImageUrl = request.ImageUrl;
                variant.PriceOverride = request.PriceOverride;
                variant.IsAvailable = request.IsAvailable;
                variant.IsFeatured = request.IsFeatured;
                variant.DisplayOrder = request.DisplayOrder;
                variant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated variant {VariantId} for product {ProductId}", id, productId);

                return Ok(new
                {
                    variant.Id,
                    variant.ProductId,
                    variant.Name,
                    variant.Sku,
                    variant.StockQuantity,
                    variant.ImageUrl,
                    variant.PriceOverride,
                    variant.IsAvailable,
                    variant.IsFeatured,
                    variant.DisplayOrder,
                    variant.CreatedAt,
                    variant.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating variant {VariantId} for product {ProductId}", id, productId);
                return StatusCode(500, new { message = "Error updating variant" });
            }
        }

        // DELETE: api/admin/products/{productId}/variants/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVariant(int productId, int id)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == id && v.ProductId == productId);

                if (variant == null)
                    return NotFound(new { message = "Variant not found" });

                _context.ProductVariants.Remove(variant);

                // Check if this was the last variant - update HasVariants flag
                var remainingVariants = await _context.ProductVariants
                    .CountAsync(v => v.ProductId == productId && v.Id != id);

                if (remainingVariants == 0)
                {
                    var product = await _context.Products.FindAsync(productId);
                    if (product != null)
                    {
                        product.HasVariants = false;
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted variant {VariantId} for product {ProductId}", id, productId);

                return Ok(new { message = "Variant deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting variant {VariantId} for product {ProductId}", id, productId);
                return StatusCode(500, new { message = "Error deleting variant" });
            }
        }
    }

    public class ProductVariantRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public int StockQuantity { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? PriceOverride { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
