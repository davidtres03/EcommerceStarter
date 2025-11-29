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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductImageService _productImageService;
        private readonly IStoredImageService _storedImageService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context,
            IProductImageService productImageService,
            IStoredImageService storedImageService,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _productImageService = productImageService;
            _storedImageService = storedImageService;
            _logger = logger;
        }

        [BindProperty]
        public ProductInputModel Product { get; set; } = new();

        public List<SelectListItem> Categories { get; set; } = new();

        public class ProductInputModel
        {
            [Required]
            public string Name { get; set; } = string.Empty;

            [Required]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Range(0.01, 10000)]
            public decimal Price { get; set; }

            // New foreign key properties (these are what we use now)
            [Required]
            [Display(Name = "Category")]
            public int? CategoryId { get; set; }

            [Required]
            [Display(Name = "Sub Category")]
            public int? SubCategoryId { get; set; }

            [Required]
            [Range(0, 10000)]
            [Display(Name = "Stock Quantity")]
            public int StockQuantity { get; set; }

            [Display(Name = "Inventory Status")]
            public InventoryStatus InventoryStatus { get; set; } = InventoryStatus.InStock;

            [Display(Name = "Image URL")]
            public string? ImageUrl { get; set; }

            [Display(Name = "Product Image ID (Stored)")]
            public Guid? ProductImageId { get; set; }

            [Display(Name = "Cloudinary Public ID")]
            public string? CloudinaryPublicId { get; set; }

            [Display(Name = "Featured Product")]
            public bool IsFeatured { get; set; } = false;
        }

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
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

        // AJAX endpoint for instant image upload to Cloudinary
        public async Task<IActionResult> OnPostUploadImageAsync(IFormFile imageFile)
        {
            try
            {
                if (imageFile == null)
                    return new JsonResult(new { success = false, message = "No file selected" });

                // Upload single image using ProductImageService (validates automatically)
                var result = await _productImageService.UploadProductImageAsync(imageFile);
                
                if (!result.Success)
                    return new JsonResult(new { success = false, message = result.ErrorMessage ?? "Upload failed" });

                // Store Cloudinary URL in StoredImages (encrypted)
                var imageId = await _storedImageService.SaveCloudinaryUrlAsync(
                    result.Url!,
                    imageFile.FileName,
                    imageFile.ContentType,
                    (int)(imageFile.Length),
                    "products",
                    $"Product:{imageFile.FileName}",
                    User.Identity?.Name
                );

                _logger.LogInformation("Product image uploaded to Cloudinary: {PublicId}, stored in StoredImages: {ImageId}", result.PublicId, imageId);

                // Return both the URL and image ID for FK reference
                return new JsonResult(new { 
                    success = true, 
                    message = "Image uploaded successfully to Cloudinary!", 
                    url = result.Url,
                    imageId = imageId.ToString(),
                    cloudinaryPublicId = result.PublicId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image to Cloudinary");
                return new JsonResult(new { success = false, message = "Upload failed. Please try again." });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            // Get category and subcategory names for legacy properties
            string categoryName = "";
            string subCategoryName = "";

            if (Product.CategoryId.HasValue)
            {
                var category = await _context.Categories.FindAsync(Product.CategoryId.Value);
                categoryName = category?.Name ?? "";
            }

            if (Product.SubCategoryId.HasValue)
            {
                var subCategory = await _context.SubCategories.FindAsync(Product.SubCategoryId.Value);
                subCategoryName = subCategory?.Name ?? "";
            }

            var product = new Product
            {
                Name = Product.Name,
                Description = Product.Description,
                Price = Product.Price,
                Category = categoryName,
                SubCategory = subCategoryName,
                CategoryId = Product.CategoryId,
                SubCategoryId = Product.SubCategoryId,
                StockQuantity = Product.StockQuantity,
                InventoryStatus = Product.InventoryStatus,
                ImageUrl = Product.ImageUrl ?? "/images/placeholder.jpg",
                ProductImageId = Product.ProductImageId,
                CloudinaryPublicId = Product.CloudinaryPublicId,
                IsFeatured = Product.IsFeatured
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Product created successfully!";

            return RedirectToPage("./Index");
        }
    }
}
