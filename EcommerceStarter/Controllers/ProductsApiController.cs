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
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsApiController> _logger;

        public ProductsApiController(ApplicationDbContext context, ILogger<ProductsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null)
        {
            try
            {
                var query = _context.Products.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => 
                        p.Name.Contains(search) || 
                        p.Description.Contains(search) ||
                        p.Category.Contains(search));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Description,
                        p.Price,
                        StockQuantity = p.StockQuantity,
                        p.ImageUrl,
                        p.Category,
                        IsActive = p.InventoryStatus == InventoryStatus.InStock,
                        p.IsFeatured,
                        CreatedAt = (DateTime?)null,
                        UpdatedAt = (DateTime?)null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    products,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products");
                return StatusCode(500, new { message = "Error fetching products" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound(new { message = "Product not found" });

                return Ok(new
                {
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price,
                    StockQuantity = product.StockQuantity,
                    product.ImageUrl,
                    product.Category,
                    IsActive = product.InventoryStatus == InventoryStatus.InStock,
                    product.IsFeatured,
                    product.HasVariants,
                    Variants = product.Variants.Select(v => new
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
                    }).ToList(),
                    CreatedAt = (DateTime?)null,
                    UpdatedAt = (DateTime?)null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product {ProductId}", id);
                return StatusCode(500, new { message = "Error fetching product" });
            }
        }
    }
}
