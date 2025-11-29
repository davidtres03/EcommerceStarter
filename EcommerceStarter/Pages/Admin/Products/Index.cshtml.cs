using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Products
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<SubCategory> SubCategories { get; set; } = new();
        public string? ActiveTab { get; set; }

        public async Task OnGetAsync(string? tab)
        {
            ActiveTab = tab ?? "products";

            if (ActiveTab == "products")
            {
                // Only load products when on products tab
                Products = await _context.Products
                    .Include(p => p.CategoryNavigation)
                    .Include(p => p.SubCategoryNavigation)
                    .Include(p => p.Variants)
                    .ToListAsync();
            }
            else if (ActiveTab == "categories")
            {
                // Only load categories and subcategories when on categories tab
                Categories = await _context.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                SubCategories = await _context.SubCategories
                    .Include(sc => sc.Category)
                    .OrderBy(sc => sc.DisplayOrder)
                    .ToListAsync();
            }
        }

        // Category Management Handlers
        public async Task<IActionResult> OnPostAddCategoryAsync(string categoryName, string? categoryDescription, string categoryIcon, int categoryOrder, bool categoryIsEnabled)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return RedirectToPage(new { tab = "categories" });
            }

            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == categoryName);

            if (existingCategory != null)
            {
                TempData["ErrorMessage"] = $"Category '{categoryName}' already exists.";
                return RedirectToPage(new { tab = "categories" });
            }

            var category = new Category
            {
                Name = categoryName,
                Description = categoryDescription,
                IconClass = string.IsNullOrWhiteSpace(categoryIcon) ? "bi-tag" : categoryIcon,
                DisplayOrder = categoryOrder,
                IsEnabled = categoryIsEnabled
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{categoryName}' created successfully.";
            return RedirectToPage(new { tab = "categories" });
        }

        public async Task<IActionResult> OnPostEditCategoryAsync(int categoryId, string categoryName, string? categoryDescription, string categoryIcon, int categoryOrder, bool categoryIsEnabled)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToPage(new { tab = "categories" });
            }

            category.Name = categoryName;
            category.Description = categoryDescription;
            category.IconClass = string.IsNullOrWhiteSpace(categoryIcon) ? "bi-tag" : categoryIcon;
            category.DisplayOrder = categoryOrder;
            category.IsEnabled = categoryIsEnabled;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{categoryName}' updated successfully.";
            return RedirectToPage(new { tab = "categories" });
        }

        public async Task<IActionResult> OnPostToggleCategoryAsync(int categoryId, bool isEnabled)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound();
            }

            category.IsEnabled = isEnabled;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteCategoryAsync(int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToPage(new { tab = "categories" });
            }

            if (category.Products.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because it has {category.Products.Count} product(s) assigned to it.";
                return RedirectToPage(new { tab = "categories" });
            }

            // Delete subcategories first
            var subCategories = await _context.SubCategories
                .Where(sc => sc.CategoryId == categoryId)
                .ToListAsync();

            _context.SubCategories.RemoveRange(subCategories);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Category '{category.Name}' deleted successfully.";
            return RedirectToPage(new { tab = "categories" });
        }

        // SubCategory Management Handlers
        public async Task<IActionResult> OnPostAddSubCategoryAsync(int parentCategoryId, string subCategoryName, string? subCategoryDescription, int subCategoryOrder, bool subCategoryIsEnabled)
        {
            if (string.IsNullOrWhiteSpace(subCategoryName))
            {
                TempData["ErrorMessage"] = "Subcategory name is required.";
                return RedirectToPage(new { tab = "categories" });
            }

            var category = await _context.Categories.FindAsync(parentCategoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Parent category not found.";
                return RedirectToPage(new { tab = "categories" });
            }

            var existingSubCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.CategoryId == parentCategoryId && sc.Name == subCategoryName);

            if (existingSubCategory != null)
            {
                TempData["ErrorMessage"] = $"Subcategory '{subCategoryName}' already exists in this category.";
                return RedirectToPage(new { tab = "categories" });
            }

            var subCategory = new SubCategory
            {
                Name = subCategoryName,
                Description = subCategoryDescription,
                IconClass = "bi-tag-fill", // Default icon
                DisplayOrder = subCategoryOrder,
                CategoryId = parentCategoryId,
                IsEnabled = subCategoryIsEnabled
            };

            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subcategory '{subCategoryName}' created successfully.";
            return RedirectToPage(new { tab = "categories" });
        }

        public async Task<IActionResult> OnPostEditSubCategoryAsync(int subCategoryId, int parentCategoryId, string subCategoryName, string? subCategoryDescription, int subCategoryOrder, bool subCategoryIsEnabled)
        {
            var subCategory = await _context.SubCategories.FindAsync(subCategoryId);
            if (subCategory == null)
            {
                TempData["ErrorMessage"] = "Subcategory not found.";
                return RedirectToPage(new { tab = "categories" });
            }

            subCategory.Name = subCategoryName;
            subCategory.Description = subCategoryDescription;
            subCategory.DisplayOrder = subCategoryOrder;
            subCategory.CategoryId = parentCategoryId;
            subCategory.IsEnabled = subCategoryIsEnabled;
            subCategory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subcategory '{subCategoryName}' updated successfully.";
            return RedirectToPage(new { tab = "categories" });
        }

        public async Task<IActionResult> OnPostToggleSubCategoryAsync(int subCategoryId, bool isEnabled)
        {
            var subCategory = await _context.SubCategories.FindAsync(subCategoryId);
            if (subCategory == null)
            {
                return NotFound();
            }

            subCategory.IsEnabled = isEnabled;
            subCategory.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteSubCategoryAsync(int subCategoryId)
        {
            var subCategory = await _context.SubCategories
                .Include(sc => sc.Products)
                .FirstOrDefaultAsync(sc => sc.Id == subCategoryId);

            if (subCategory == null)
            {
                TempData["ErrorMessage"] = "Subcategory not found.";
                return RedirectToPage(new { tab = "categories" });
            }

            if (subCategory.Products.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete subcategory '{subCategory.Name}' because it has {subCategory.Products.Count} product(s) assigned to it.";
                return RedirectToPage(new { tab = "categories" });
            }

            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Subcategory '{subCategory.Name}' deleted successfully.";
            return RedirectToPage(new { tab = "categories" });
        }
    }
}
