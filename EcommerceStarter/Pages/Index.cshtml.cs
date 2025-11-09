using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EcommerceStarter.Models;
using EcommerceStarter.Data;
using EcommerceStarter.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ISiteSettingsService _siteSettingsService;

    public IndexModel(ApplicationDbContext context, ISiteSettingsService siteSettingsService)
    {
        _context = context;
        _siteSettingsService = siteSettingsService;
    }

    public List<Product> Products { get; private set; } = new();
    public List<Category> ActiveCategories { get; private set; } = new();
    public string? CurrentCategory { get; private set; }
    public string? CurrentSubCategory { get; private set; }
    public SiteSettings Settings { get; private set; } = new();

    public async Task OnGetAsync(string? category = null, string? subcategory = null)
    {
        CurrentCategory = category;
        CurrentSubCategory = subcategory;

        // Get site settings for hero image
        Settings = await _siteSettingsService.GetSettingsAsync();

        var query = _context.Products
            .Include(p => p.Variants)
            .AsQueryable();

        if (!string.IsNullOrEmpty(category))
        {
            // Use ToLower() for case-insensitive comparison (translatable to SQL)
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        if (!string.IsNullOrEmpty(subcategory))
        {
            // Use ToLower() for case-insensitive comparison (translatable to SQL)
            query = query.Where(p => p.SubCategory.ToLower() == subcategory.ToLower());
        }

        var allProducts = await query.ToListAsync();

        // Filter out Out of Stock products
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
        }).ToList();

        Products = availableProducts.OrderBy(p => p.Category).ThenBy(p => p.Name).ToList();

        // Get active categories that have products assigned (regardless of stock status)
        ActiveCategories = await _context.Categories
            .Where(c => c.IsEnabled && c.Products.Any())
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
    }
}