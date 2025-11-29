using System.ComponentModel.DataAnnotations;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Products
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUploadService _imageUploadService;
        private readonly IProductImageService _productImageService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            IImageUploadService imageUploadService,
            IProductImageService productImageService,
            ILogger<EditModel> logger)
        {
            _context = context;
            _imageUploadService = imageUploadService;
            _productImageService = productImageService;
            _logger = logger;
        }

        [BindProperty]
        public ProductInputModel Product { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> SubCategories { get; set; } = new();
        public List<ProductVariant> ProductVariants { get; set; } = new();

        public class ProductInputModel
        {
            public int Id { get; set; }

            [Required]
            public string Name { get; set; } = string.Empty;

            [Required]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Range(0.01, 10000)]
            public decimal Price { get; set; }

            // Legacy string properties for backward compatibility
            public string Category { get; set; } = string.Empty;

            public string SubCategory { get; set; } = string.Empty;

            // New foreign key properties
            [Display(Name = "Category")]
            public int? CategoryId { get; set; }

            [Display(Name = "Sub Category")]
            public int? SubCategoryId { get; set; }

            [Display(Name = "Image URL")]
            public string? ImageUrl { get; set; }

            [Display(Name = "Featured Product")]
            public bool IsFeatured { get; set; } = false;

            [Display(Name = "Stock Status")]
            public InventoryStatus InventoryStatus { get; set; } = InventoryStatus.InStock;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.CategoryNavigation)
                .Include(p => p.SubCategoryNavigation)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            Product = new ProductInputModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Category = product.Category,
                SubCategory = product.SubCategory,
                CategoryId = product.CategoryId,
                SubCategoryId = product.SubCategoryId,
                ImageUrl = product.ImageUrl,
                IsFeatured = product.IsFeatured,
                InventoryStatus = product.InventoryStatus // Map InventoryStatus
            };

            ProductVariants = product.Variants.OrderBy(v => v.DisplayOrder).ToList();

            await LoadCategoriesAsync(product.CategoryId);

            return Page();
        }

        private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }).ToList();

            if (selectedCategoryId.HasValue)
            {
                var subCategories = await _context.SubCategories
                    .Where(sc => sc.CategoryId == selectedCategoryId.Value && sc.IsEnabled)
                    .OrderBy(sc => sc.DisplayOrder)
                    .ToListAsync();

                SubCategories = subCategories.Select(sc => new SelectListItem
                {
                    Value = sc.Id.ToString(),
                    Text = sc.Name
                }).ToList();
            }
        }

        // AJAX endpoint to get subcategories for a category
        public async Task<IActionResult> OnGetSubCategoriesAsync(int categoryId)
        {
            var subCategories = await _context.SubCategories
                .Where(sc => sc.CategoryId == categoryId && sc.IsEnabled)
                .OrderBy(sc => sc.DisplayOrder)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return new JsonResult(subCategories);
        }

        // AJAX endpoint for instant image upload
        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile imageFile, int productId)
        {
            try
            {
                if (imageFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                if (!_imageUploadService.IsValidImage(imageFile))
                    return new JsonResult(new { success = false, message = "Invalid file. Use JPG, PNG, GIF, or SVG (max 5MB)" });

                var imageUrl = await _imageUploadService.UploadImageAsync(imageFile, "images/products");
                
                // Extract imageId from URL format: /images/stored/{guid}
                var imageId = Guid.Parse(imageUrl.Replace("/images/stored/", ""));
                
                // Update product with FK to StoredImage
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.ProductImageId = imageId;
                    product.ImageUrl = imageUrl;
                    await _context.SaveChangesAsync();
                }
                
                _logger.LogInformation("Product image uploaded: {Url}, FK saved: {ImageId}", imageUrl, imageId);

                return new JsonResult(new { success = true, message = "Image uploaded successfully!", url = imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove validation for legacy string properties since they're optional now
            ModelState.Remove("Product.Category");
            ModelState.Remove("Product.SubCategory");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid on product edit");
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Validation error for {Key}: {Error}", state.Key, error.ErrorMessage);
                    }
                }
                await LoadCategoriesAsync(Product.CategoryId);
                return Page();
            }

            var product = await _context.Products.FindAsync(Product.Id);

            if (product == null)
            {
                _logger.LogWarning("Product not found: {ProductId}", Product.Id);
                return NotFound();
            }

            _logger.LogInformation("Updating product {ProductId}: {ProductName}", Product.Id, Product.Name);

            product.Name = Product.Name;
            product.Description = Product.Description;
            product.Price = Product.Price;
            product.ImageUrl = Product.ImageUrl ?? "/images/placeholder.jpg";
            product.IsFeatured = Product.IsFeatured;
            product.InventoryStatus = Product.InventoryStatus;

            // If setting to Out of Stock, ensure featured is false
            if (product.InventoryStatus == InventoryStatus.OutOfStock)
            {
                product.IsFeatured = false;
            }

            // Update category relationships
            product.CategoryId = Product.CategoryId;
            product.SubCategoryId = Product.SubCategoryId;

            // Update legacy string properties for backward compatibility
            if (Product.CategoryId.HasValue)
            {
                var category = await _context.Categories.FindAsync(Product.CategoryId.Value);
                product.Category = category?.Name ?? "";
            }
            else
            {
                product.Category = "";
            }

            if (Product.SubCategoryId.HasValue)
            {
                var subCategory = await _context.SubCategories.FindAsync(Product.SubCategoryId.Value);
                product.SubCategory = subCategory?.Name ?? "";
            }
            else
            {
                product.SubCategory = "";
            }

            try
            {
                _context.Products.Update(product);
                var changeCount = await _context.SaveChangesAsync();
                
                _logger.LogInformation("Product {ProductId} updated successfully. Changes saved: {ChangeCount}", Product.Id, changeCount);
                
                TempData["SuccessMessage"] = "Product updated successfully!";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating product {ProductId}", Product.Id);
                
                if (!await ProductExists(Product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", Product.Id);
                throw;
            }

            return RedirectToPage("./Index");
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }

        // AJAX endpoint to get variants for a product
        public async Task<IActionResult> OnGetVariantsAsync(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .OrderBy(v => v.DisplayOrder)
                .Select(v => new
                {
                    id = v.Id,
                    name = v.Name,
                    sku = v.Sku,
                    stock = v.StockQuantity,
                    imageUrl = v.ImageUrl,
                    priceOverride = v.PriceOverride,
                    isAvailable = v.IsAvailable,
                    isFeatured = v.IsFeatured,
                    displayOrder = v.DisplayOrder
                })
                .ToListAsync();

            return new JsonResult(variants);
        }

        // AJAX endpoint to add a variant
        public async Task<IActionResult> OnPostAddVariantAsync(int productId, string name, int stock, string? imageUrl, decimal? priceOverride, string? sku)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new JsonResult(new { success = false, message = "Variant name is required" });

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return new JsonResult(new { success = false, message = "Product not found" });

                var variants = await _context.ProductVariants
                    .Where(v => v.ProductId == productId)
                    .ToListAsync();
                var maxOrder = variants.Count > 0 ? variants.Max(v => v.DisplayOrder) : 0;

                var variant = new ProductVariant
                {
                    ProductId = productId,
                    Name = name.Trim(),
                    Sku = sku?.Trim(),
                    StockQuantity = Math.Max(0, stock),
                    ImageUrl = imageUrl,
                    PriceOverride = priceOverride,
                    DisplayOrder = maxOrder + 1,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ProductVariants.Add(variant);
                
                // ? FIX: Set HasVariants to true when adding a variant
                if (!product.HasVariants)
                {
                    product.HasVariants = true;
                    _context.Products.Update(product);
                }
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant added to product {ProductId}: {VariantName}", productId, name);

                return new JsonResult(new
                {
                    success = true,
                    message = "Variant added successfully!",
                    variant = new
                    {
                        id = variant.Id,
                        name = variant.Name,
                        sku = variant.Sku,
                        stock = variant.StockQuantity,
                        imageUrl = variant.ImageUrl,
                        priceOverride = variant.PriceOverride,
                        isAvailable = variant.IsAvailable,
                        displayOrder = variant.DisplayOrder
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding variant");
                return new JsonResult(new { success = false, message = "Error adding variant" });
            }
        }

        // AJAX endpoint to update a variant
        public async Task<IActionResult> OnPostUpdateVariantAsync(int variantId, string name, int stock, string? imageUrl, decimal? priceOverride, string? sku, bool isAvailable)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new JsonResult(new { success = false, message = "Variant name is required" });

                var variant = await _context.ProductVariants.FindAsync(variantId);
                if (variant == null)
                    return new JsonResult(new { success = false, message = "Variant not found" });

                variant.Name = name.Trim();
                variant.Sku = sku?.Trim();
                variant.StockQuantity = Math.Max(0, stock);
                variant.ImageUrl = imageUrl;
                variant.PriceOverride = priceOverride;
                variant.IsAvailable = isAvailable;
                variant.UpdatedAt = DateTime.UtcNow;

                _context.ProductVariants.Update(variant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant {VariantId} updated", variantId);

                return new JsonResult(new { success = true, message = "Variant updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating variant");
                return new JsonResult(new { success = false, message = "Error updating variant" });
            }
        }

        // AJAX endpoint to delete a variant
        public async Task<IActionResult> OnPostDeleteVariantAsync(int variantId)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == variantId);
                    
                if (variant == null)
                    return new JsonResult(new { success = false, message = "Variant not found" });

                var productId = variant.ProductId;
                
                _context.ProductVariants.Remove(variant);
                
                // ? FIX: Check if this is the last variant, and if so, set HasVariants to false
                var remainingVariantCount = await _context.ProductVariants
                    .CountAsync(v => v.ProductId == productId && v.Id != variantId);
                    
                if (remainingVariantCount == 0 && variant.Product != null)
                {
                    variant.Product.HasVariants = false;
                    _context.Products.Update(variant.Product);
                }
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant {VariantId} deleted", variantId);

                return new JsonResult(new { success = true, message = "Variant deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting variant");
                return new JsonResult(new { success = false, message = "Error deleting variant" });
            }
        }

        // AJAX endpoint to edit a variant
        public async Task<IActionResult> OnPostEditVariantAsync(int variantId, string name, int stock, string? sku, decimal? priceOverride, bool isAvailable, string? imageUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return new JsonResult(new { success = false, message = "Variant name is required" });

                var variant = await _context.ProductVariants.FindAsync(variantId);
                if (variant == null)
                    return new JsonResult(new { success = false, message = "Variant not found" });

                variant.Name = name.Trim();
                variant.Sku = sku?.Trim();
                variant.StockQuantity = Math.Max(0, stock);
                variant.PriceOverride = priceOverride;
                variant.IsAvailable = isAvailable;
                variant.ImageUrl = imageUrl?.Trim();
                variant.UpdatedAt = DateTime.UtcNow;

                _context.ProductVariants.Update(variant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant {VariantId} updated", variantId);

                return new JsonResult(new { success = true, message = "Variant updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing variant");
                return new JsonResult(new { success = false, message = "Error editing variant" });
            }
        }

        // AJAX endpoint to toggle featured status (only one per product can be featured)
        public async Task<IActionResult> OnPostToggleFeaturedAsync(int variantId, bool isFeatured)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == variantId);

                if (variant == null)
                    return new JsonResult(new { success = false, message = "Variant not found" });

                var productId = variant.ProductId;

                if (isFeatured)
                {
                    // If setting this variant as featured, unfeature all other variants for this product
                    var otherVariants = await _context.ProductVariants
                        .Where(v => v.ProductId == productId && v.Id != variantId && v.IsFeatured)
                        .ToListAsync();

                    foreach (var other in otherVariants)
                    {
                        other.IsFeatured = false;
                        _context.ProductVariants.Update(other);
                    }

                    variant.IsFeatured = true;
                }
                else
                {
                    // Simply unfeature this variant
                    variant.IsFeatured = false;
                }

                _context.ProductVariants.Update(variant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Variant {VariantId} featured status toggled to {Status}", variantId, isFeatured);

                return new JsonResult(new
                {
                    success = true,
                    message = isFeatured ? "Variant marked as featured!" : "Variant unmarked as featured!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling featured status");
                return new JsonResult(new { success = false, message = "Error toggling featured status" });
            }
        }

        // AJAX endpoint to reorder variants
        public async Task<IActionResult> OnPostReorderVariantsAsync(List<int> variantIds)
        {
            try
            {
                for (int i = 0; i < variantIds.Count; i++)
                {
                    var variant = await _context.ProductVariants.FindAsync(variantIds[i]);
                    if (variant != null)
                    {
                        variant.DisplayOrder = i;
                        _context.ProductVariants.Update(variant);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Variants reordered");

                return new JsonResult(new { success = true, message = "Variants reordered successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering variants");
                return new JsonResult(new { success = false, message = "Error reordering variants" });
            }
        }

        // ========== VARIANT ATTRIBUTES HANDLERS ==========

        /// <summary>
        /// Get all variant attributes for a product (Color, Size, Material, etc.)
        /// </summary>
        public async Task<IActionResult> OnGetAttributesAsync(int productId)
        {
            try
            {
                var attributes = await _context.VariantAttributes
                    .Where(va => va.ProductId == productId)
                    .OrderBy(va => va.DisplayOrder)
                    .ToListAsync();

                var result = attributes.Select(va => new
                {
                    id = va.Id,
                    name = va.Name,
                    values = va.Values,
                    valuesList = va.Values.Split(',').Select(v => v.Trim()).ToList(),
                    displayOrder = va.DisplayOrder
                }).ToList();

                return new JsonResult(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading attributes");
                return new JsonResult(new { success = false, message = "Error loading attributes" });
            }
        }

        /// <summary>
        /// Add a new variant attribute (Color, Size, etc.)
        /// </summary>
        public async Task<IActionResult> OnPostAddAttributeAsync(int productId, string name, string values)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(values))
                    return new JsonResult(new { success = false, message = "Attribute name and values are required" });

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return new JsonResult(new { success = false, message = "Product not found" });

                // Check if attribute already exists
                var existingAttr = await _context.VariantAttributes
                    .FirstOrDefaultAsync(va => va.ProductId == productId && va.Name.ToLower() == name.ToLower());
                if (existingAttr != null)
                    return new JsonResult(new { success = false, message = "Attribute already exists" });

                var attributes = await _context.VariantAttributes
                    .Where(va => va.ProductId == productId)
                    .ToListAsync();
                var maxOrder = attributes.Count > 0 ? attributes.Max(va => va.DisplayOrder) : 0;

                var attribute = new VariantAttribute
                {
                    ProductId = productId,
                    Name = name.Trim(),
                    Values = values.Trim(),
                    DisplayOrder = maxOrder + 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VariantAttributes.Add(attribute);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attribute added to product {ProductId}: {AttributeName}", productId, name);

                return new JsonResult(new
                {
                    success = true,
                    message = "Attribute added successfully!",
                    attribute = new
                    {
                        id = attribute.Id,
                        name = attribute.Name,
                        values = attribute.Values,
                        valuesList = attribute.ValuesList,
                        displayOrder = attribute.DisplayOrder
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding attribute");
                return new JsonResult(new { success = false, message = "Error adding attribute" });
            }
        }

        /// <summary>
        /// Update a variant attribute
        /// </summary>
        public async Task<IActionResult> OnPostUpdateAttributeAsync(int attributeId, string name, string values)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(values))
                    return new JsonResult(new { success = false, message = "Attribute name and values are required" });

                var attribute = await _context.VariantAttributes.FindAsync(attributeId);
                if (attribute == null)
                    return new JsonResult(new { success = false, message = "Attribute not found" });

                attribute.Name = name.Trim();
                attribute.Values = values.Trim();
                attribute.UpdatedAt = DateTime.UtcNow;

                _context.VariantAttributes.Update(attribute);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attribute {AttributeId} updated", attributeId);

                return new JsonResult(new { success = true, message = "Attribute updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating attribute");
                return new JsonResult(new { success = false, message = "Error updating attribute" });
            }
        }

        /// <summary>
        /// Delete a variant attribute
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAttributeAsync(int attributeId)
        {
            try
            {
                var attribute = await _context.VariantAttributes.FindAsync(attributeId);
                if (attribute == null)
                    return new JsonResult(new { success = false, message = "Attribute not found" });

                // Delete all attribute values for this attribute
                var attributeValues = await _context.VariantAttributeValues
                    .Where(vav => vav.VariantAttributeId == attributeId)
                    .ToListAsync();

                _context.VariantAttributeValues.RemoveRange(attributeValues);
                _context.VariantAttributes.Remove(attribute);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Attribute {AttributeId} deleted", attributeId);

                return new JsonResult(new { success = true, message = "Attribute deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting attribute");
                return new JsonResult(new { success = false, message = "Error deleting attribute" });
            }
        }

        /// <summary>
        /// Upload product image
        /// </summary>
        public async Task<IActionResult> OnPostUploadProductImageAsync(IFormFile file, int productId)
        {
            try
            {
                if (file == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                // Use ProductImageService for Cloudinary upload
                var result = await _productImageService.UploadProductImageAsync(file, productId);

                if (!result.Success)
                {
                    _logger.LogError("Product image upload failed for product {ProductId}: {Error}", productId, result.ErrorMessage);
                    return new JsonResult(new { success = false, message = result.ErrorMessage });
                }

                _logger.LogInformation("Product image uploaded to Cloudinary for product {ProductId}: {Url}", productId, result.Url);

                return new JsonResult(new
                {
                    success = true,
                    message = "Image uploaded successfully to Cloudinary!",
                    imageUrl = result.Url,
                    publicId = result.PublicId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        /// <summary>
        /// Upload variant image (works for both new and existing variants)
        /// </summary>
        public async Task<IActionResult> OnPostUploadVariantImageAsync(IFormFile file, int? variantId = null)
        {
            try
            {
                if (file == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                // Use ProductImageService for Cloudinary upload
                var result = await _productImageService.UploadProductImageAsync(file, variantId ?? 0);

                if (!result.Success)
                {
                    _logger.LogError("Variant image upload failed for variant {VariantId}: {Error}", variantId, result.ErrorMessage);
                    return new JsonResult(new { success = false, message = result.ErrorMessage });
                }

                if (variantId.HasValue)
                {
                    _logger.LogInformation("Variant image uploaded to Cloudinary for variant {VariantId}: {Url}", variantId, result.Url);
                }
                else
                {
                    _logger.LogInformation("Variant image uploaded to Cloudinary (new variant): {Url}", result.Url);
                }

                return new JsonResult(new
                {
                    success = true,
                    message = "Image uploaded successfully to Cloudinary!",
                    imageUrl = result.Url,
                    publicId = result.PublicId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading variant image");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }
    }
}
