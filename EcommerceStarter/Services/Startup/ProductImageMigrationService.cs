using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services.Startup
{
    /// <summary>
    /// One-time startup job to migrate legacy Product.ImageUrl (Cloudinary) into unified encrypted StoredImages.
    /// Idempotent: only processes products missing ProductImageId and having a non-empty ImageUrl.
    /// </summary>
    public class ProductImageMigrationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IStoredImageService _storedImageService;
        private readonly ILogger<ProductImageMigrationService> _logger;

        public ProductImageMigrationService(
            ApplicationDbContext db,
            IStoredImageService storedImageService,
            ILogger<ProductImageMigrationService> logger)
        {
            _db = db;
            _storedImageService = storedImageService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            // Quick check: any products needing migration?
            var needsMigration = await _db.Products
                .AsNoTracking()
                .AnyAsync(p => p.ProductImageId == null && !string.IsNullOrWhiteSpace(p.ImageUrl), ct);

            if (!needsMigration)
            {
                _logger.LogInformation("[Startup] Product image migration: no work needed.");
                return;
            }

            _logger.LogInformation("[Startup] Product image migration: starting…");

            // Process in small batches to avoid long transactions
            const int batchSize = 100;
            while (true)
            {
                var batch = await _db.Products
                    .Where(p => p.ProductImageId == null && !string.IsNullOrWhiteSpace(p.ImageUrl))
                    .OrderBy(p => p.Id)
                    .Take(batchSize)
                    .ToListAsync(ct);

                if (batch.Count == 0)
                    break;

                foreach (var product in batch)
                {
                    try
                    {
                        var url = product.ImageUrl.Trim();
                        // Best-effort content type detection from URL
                        var contentType = url.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
                            : url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg"
                            : "application/octet-stream";

                        var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
                        var usedBy = $"Product:{product.Id}";

                        var storedId = await _storedImageService.SaveCloudinaryUrlAsync(
                            url: url,
                            fileName: string.IsNullOrWhiteSpace(fileName) ? $"product-{product.Id}" : fileName,
                            contentType: contentType,
                            fileSize: 0,
                            category: "products",
                            usedBy: usedBy,
                            uploadedBy: "startup-migration");

                        product.ProductImageId = storedId;
                        _db.Products.Update(product);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Startup] Failed migrating image for Product {ProductId}. Continuing.", product.Id);
                    }
                }

                try
                {
                    await _db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Startup] Error saving product image migration batch.");
                }
            }

            _logger.LogInformation("[Startup] Product image migration: completed.");
        }
    }
}
