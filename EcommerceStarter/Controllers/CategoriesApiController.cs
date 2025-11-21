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
    [Route("api/categories")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesApiController> _logger;

        public CategoriesApiController(ApplicationDbContext context, ILogger<CategoriesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetCategories([FromQuery] bool includeDisabled = false)
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                if (!includeDisabled)
                {
                    query = query.Where(c => c.IsEnabled);
                }

                var categories = await query
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                // Get subcategories separately
                var categoryIds = categories.Select(c => c.Id).ToList();
                var subCategoriesQuery = _context.SubCategories
                    .Where(sc => categoryIds.Contains(sc.CategoryId));

                if (!includeDisabled)
                {
                    subCategoriesQuery = subCategoriesQuery.Where(sc => sc.IsEnabled);
                }

                var subCategories = await subCategoriesQuery
                    .OrderBy(sc => sc.DisplayOrder)
                    .ThenBy(sc => sc.Name)
                    .ToListAsync();

                var result = categories.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.IconClass,
                    c.IsEnabled,
                    c.DisplayOrder,
                    c.CreatedAt,
                    c.UpdatedAt,
                    SubCategories = subCategories
                        .Where(sc => sc.CategoryId == c.Id)
                        .Select(sc => new
                        {
                            sc.Id,
                            sc.Name,
                            sc.Description,
                            sc.IconClass,
                            sc.IsEnabled,
                            sc.DisplayOrder,
                            sc.CreatedAt,
                            sc.UpdatedAt
                        })
                        .ToList()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = result,
                    count = result.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { success = false, message = "Error retrieving categories" });
            }
        }

        // GET: api/categories/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Where(c => c.Id == id)
                    .FirstOrDefaultAsync();

                if (category == null)
                {
                    return NotFound(new { success = false, message = "Category not found" });
                }

                // Get subcategories separately
                var subCategories = await _context.SubCategories
                    .Where(sc => sc.CategoryId == id)
                    .OrderBy(sc => sc.DisplayOrder)
                    .ThenBy(sc => sc.Name)
                    .ToListAsync();

                var result = new
                {
                    category.Id,
                    category.Name,
                    category.Description,
                    category.IconClass,
                    category.IsEnabled,
                    category.DisplayOrder,
                    category.CreatedAt,
                    category.UpdatedAt,
                    SubCategories = subCategories.Select(sc => new
                    {
                        sc.Id,
                        sc.Name,
                        sc.Description,
                        sc.IconClass,
                        sc.IsEnabled,
                        sc.DisplayOrder,
                        sc.CreatedAt,
                        sc.UpdatedAt
                    }).ToList()
                };

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category {CategoryId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving category" });
            }
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "Category name is required" });
                }

                // Check if category with same name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower());

                if (existingCategory != null)
                {
                    return BadRequest(new { success = false, message = "A category with this name already exists" });
                }

                var category = new Category
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    IconClass = !string.IsNullOrWhiteSpace(request.IconClass) ? request.IconClass.Trim() : "bi-tag",
                    IsEnabled = request.IsEnabled ?? true,
                    DisplayOrder = request.DisplayOrder ?? 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category created: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);

                return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, new
                {
                    success = true,
                    message = "Category created successfully",
                    data = new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.IconClass,
                        category.IsEnabled,
                        category.DisplayOrder,
                        category.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { success = false, message = "Error creating category" });
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { success = false, message = "Category not found" });
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    // Check if another category with same name exists
                    var existingCategory = await _context.Categories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != id);

                    if (existingCategory != null)
                    {
                        return BadRequest(new { success = false, message = "A category with this name already exists" });
                    }

                    category.Name = request.Name.Trim();
                }

                if (request.Description != null)
                {
                    category.Description = request.Description.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.IconClass))
                {
                    category.IconClass = request.IconClass.Trim();
                }

                if (request.IsEnabled.HasValue)
                {
                    category.IsEnabled = request.IsEnabled.Value;
                }

                if (request.DisplayOrder.HasValue)
                {
                    category.DisplayOrder = request.DisplayOrder.Value;
                }

                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Category updated: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);

                return Ok(new
                {
                    success = true,
                    message = "Category updated successfully",
                    data = new
                    {
                        category.Id,
                        category.Name,
                        category.Description,
                        category.IconClass,
                        category.IsEnabled,
                        category.DisplayOrder,
                        category.CreatedAt,
                        category.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", id);
                return StatusCode(500, new { success = false, message = "Error updating category" });
            }
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    return NotFound(new { success = false, message = "Category not found" });
                }

                // Check if category has products
                var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
                if (hasProducts)
                {
                    var productCount = await _context.Products.CountAsync(p => p.CategoryId == id);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete category with associated products. Please reassign or delete products first.",
                        productCount
                    });
                }

                // Check if category has subcategories
                var hasSubCategories = await _context.SubCategories.AnyAsync(sc => sc.CategoryId == id);
                if (hasSubCategories)
                {
                    var subcategoryCount = await _context.SubCategories.CountAsync(sc => sc.CategoryId == id);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete category with subcategories. Please delete subcategories first.",
                        subcategoryCount
                    });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Category deleted: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);

                return Ok(new
                {
                    success = true,
                    message = "Category deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting category" });
            }
        }

        // POST: api/categories/{categoryId}/subcategories
        [HttpPost("{categoryId}/subcategories")]
        public async Task<IActionResult> CreateSubCategory(int categoryId, [FromBody] CreateSubCategoryRequest request)
        {
            try
            {
                var category = await _context.Categories.FindAsync(categoryId);

                if (category == null)
                {
                    return NotFound(new { success = false, message = "Parent category not found" });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { success = false, message = "Subcategory name is required" });
                }

                // Check if subcategory with same name already exists in this category
                var existingSubCategory = await _context.SubCategories
                    .FirstOrDefaultAsync(sc => sc.CategoryId == categoryId && sc.Name.ToLower() == request.Name.ToLower());

                if (existingSubCategory != null)
                {
                    return BadRequest(new { success = false, message = "A subcategory with this name already exists in this category" });
                }

                var subCategory = new SubCategory
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    IconClass = !string.IsNullOrWhiteSpace(request.IconClass) ? request.IconClass.Trim() : "bi-tag-fill",
                    IsEnabled = request.IsEnabled ?? true,
                    DisplayOrder = request.DisplayOrder ?? 0,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SubCategories.Add(subCategory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subcategory created: {SubCategoryName} (ID: {SubCategoryId}) under Category ID: {CategoryId}",
                    subCategory.Name, subCategory.Id, categoryId);

                return CreatedAtAction(nameof(GetSubCategory), new { id = subCategory.Id }, new
                {
                    success = true,
                    message = "Subcategory created successfully",
                    data = new
                    {
                        subCategory.Id,
                        subCategory.Name,
                        subCategory.Description,
                        subCategory.IconClass,
                        subCategory.IsEnabled,
                        subCategory.DisplayOrder,
                        subCategory.CategoryId,
                        CategoryName = category.Name,
                        subCategory.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subcategory for category {CategoryId}", categoryId);
                return StatusCode(500, new { success = false, message = "Error creating subcategory" });
            }
        }

        // GET: api/subcategories/{id}
        [HttpGet("~/api/subcategories/{id}")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            try
            {
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .Where(sc => sc.Id == id)
                    .Select(sc => new
                    {
                        sc.Id,
                        sc.Name,
                        sc.Description,
                        sc.IconClass,
                        sc.IsEnabled,
                        sc.DisplayOrder,
                        sc.CategoryId,
                        CategoryName = sc.Category.Name,
                        sc.CreatedAt,
                        sc.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (subCategory == null)
                {
                    return NotFound(new { success = false, message = "Subcategory not found" });
                }

                return Ok(new { success = true, data = subCategory });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subcategory {SubCategoryId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving subcategory" });
            }
        }

        // PUT: api/subcategories/{id}
        [HttpPut("~/api/subcategories/{id}")]
        public async Task<IActionResult> UpdateSubCategory(int id, [FromBody] UpdateSubCategoryRequest request)
        {
            try
            {
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .FirstOrDefaultAsync(sc => sc.Id == id);

                if (subCategory == null)
                {
                    return NotFound(new { success = false, message = "Subcategory not found" });
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    // Check if another subcategory with same name exists in the same category
                    var existingSubCategory = await _context.SubCategories
                        .FirstOrDefaultAsync(sc => sc.CategoryId == subCategory.CategoryId &&
                                                   sc.Name.ToLower() == request.Name.ToLower() &&
                                                   sc.Id != id);

                    if (existingSubCategory != null)
                    {
                        return BadRequest(new { success = false, message = "A subcategory with this name already exists in this category" });
                    }

                    subCategory.Name = request.Name.Trim();
                }

                if (request.Description != null)
                {
                    subCategory.Description = request.Description.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.IconClass))
                {
                    subCategory.IconClass = request.IconClass.Trim();
                }

                if (request.IsEnabled.HasValue)
                {
                    subCategory.IsEnabled = request.IsEnabled.Value;
                }

                if (request.DisplayOrder.HasValue)
                {
                    subCategory.DisplayOrder = request.DisplayOrder.Value;
                }

                subCategory.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subcategory updated: {SubCategoryName} (ID: {SubCategoryId})", subCategory.Name, subCategory.Id);

                return Ok(new
                {
                    success = true,
                    message = "Subcategory updated successfully",
                    data = new
                    {
                        subCategory.Id,
                        subCategory.Name,
                        subCategory.Description,
                        subCategory.IconClass,
                        subCategory.IsEnabled,
                        subCategory.DisplayOrder,
                        subCategory.CategoryId,
                        CategoryName = subCategory.Category.Name,
                        subCategory.CreatedAt,
                        subCategory.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subcategory {SubCategoryId}", id);
                return StatusCode(500, new { success = false, message = "Error updating subcategory" });
            }
        }

        // DELETE: api/subcategories/{id}
        [HttpDelete("~/api/subcategories/{id}")]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            try
            {
                var subCategory = await _context.SubCategories
                    .Include(sc => sc.Products)
                    .FirstOrDefaultAsync(sc => sc.Id == id);

                if (subCategory == null)
                {
                    return NotFound(new { success = false, message = "Subcategory not found" });
                }

                // Check if subcategory has products
                if (subCategory.Products.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cannot delete subcategory with associated products. Please reassign or delete products first.",
                        productCount = subCategory.Products.Count
                    });
                }

                _context.SubCategories.Remove(subCategory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subcategory deleted: {SubCategoryName} (ID: {SubCategoryId})", subCategory.Name, subCategory.Id);

                return Ok(new
                {
                    success = true,
                    message = "Subcategory deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subcategory {SubCategoryId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting subcategory" });
            }
        }
    }

    // Request DTOs
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public bool? IsEnabled { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class UpdateCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public bool? IsEnabled { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class CreateSubCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public bool? IsEnabled { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class UpdateSubCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public bool? IsEnabled { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
