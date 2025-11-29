using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.ViewComponents
{
    public class ActiveCategoriesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public ActiveCategoriesViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get active categories that have in-stock products
            // Only show categories where at least one product is available (StockQuantity > 0)
            var categories = await _context.Categories
                .Where(c => c.IsEnabled && c.Products.Any(p => p.StockQuantity > 0))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(categories);
        }
    }
}
