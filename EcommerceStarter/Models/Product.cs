using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStarter.Models
{
    // Models/Product.cs
    /// <summary>
    /// Enum for product inventory status
    /// </summary>
    public enum InventoryStatus
    {
        /// <summary>
        /// Product is currently in stock and available for purchase
        /// </summary>
        InStock = 0,
        
        /// <summary>
        /// Product is out of stock and not available for purchase
        /// </summary>
        OutOfStock = 1,
        
        /// <summary>
        /// Product is coming soon - displays on product page but not purchasable
        /// </summary>
        ComingSoon = 2
    }

    public class Product
    {
        public int Id { get; set; }
        
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Description (supports HTML formatting)")]
        public string Description { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Unified encrypted image storage reference for this product image
        /// When present, use this instead of legacy ImageUrl
        /// </summary>
        public Guid? ProductImageId { get; set; }

        /// <summary>
        /// Navigation to the stored image record
        /// </summary>
        public EcommerceStarter.Models.StoredImage? ProductImage { get; set; }
        
        /// <summary>
        /// Cloudinary public ID for the product image (for future deletion/updates)
        /// </summary>
        [MaxLength(255)]
        public string? CloudinaryPublicId { get; set; }
        
        // Legacy string properties - keep for backward compatibility during migration
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string SubCategory { get; set; } = string.Empty;
        
        // New foreign key relationships
        public int? CategoryId { get; set; }
        public EcommerceStarter.Models.Category? CategoryNavigation { get; set; }

        public int? SubCategoryId { get; set; }
        public EcommerceStarter.Models.SubCategory? SubCategoryNavigation { get; set; }
        
        /// <summary>
        /// DEPRECATED: Use Variants collection instead.
        /// Kept for backward compatibility but should not be used for new code.
        /// </summary>
        public int StockQuantity { get; set; }
        
        /// <summary>
        /// DEPRECATED: Inventory status is now determined by variants.
        /// Kept for backward compatibility but should not be used for new code.
        /// If a product has variants, they control availability. Otherwise, this is used.
        /// </summary>
        [Display(Name = "Inventory Status")]
        public InventoryStatus InventoryStatus { get; set; } = InventoryStatus.InStock;
        
        /// <summary>
        /// Product variants (color, size, etc.)
        /// When variants exist, they control stock and availability.
        /// </summary>
        public ICollection<EcommerceStarter.Models.ProductVariant> Variants { get; set; } = new List<EcommerceStarter.Models.ProductVariant>();

        /// <summary>
        /// Variant attributes for this product (Color, Size, Material, etc.)
        /// </summary>
        public ICollection<EcommerceStarter.Models.VariantAttribute> VariantAttributes { get; set; } = new List<EcommerceStarter.Models.VariantAttribute>();

        /// <summary>
        /// Whether this product has variants
        /// </summary>
        [Display(Name = "Has Variants")]
        public bool HasVariants { get; set; } = false;
        
        public bool IsFeatured { get; set; } = false;

        // ========== COMPUTED PROPERTIES FOR VARIANT-BASED AVAILABILITY ==========
        
        /// <summary>
        /// Get the featured variant for this product (if any).
        /// Only one variant per product can be featured at a time.
        /// </summary>
        [NotMapped]
        public ProductVariant? FeaturedVariant
        {
            get
            {
                if (Variants == null || Variants.Count == 0)
                    return null;
                
                return Variants.FirstOrDefault(v => v.IsFeatured && v.IsInStock);
            }
        }

        /// <summary>
        /// Get total stock across all variants. If no variants, use legacy StockQuantity.
        /// </summary>
        [NotMapped]
        public int TotalAvailableStock
        {
            get
            {
                if (Variants == null || Variants.Count == 0)
                    return StockQuantity;
                
                return Variants.Where(v => v.IsAvailable).Sum(v => v.StockQuantity);
            }
        }

        /// <summary>
        /// Check if product is available (has variants in stock OR legacy status is InStock and has stock).
        /// If variants exist, they determine availability. Otherwise, use legacy status.
        /// </summary>
        [NotMapped]
        public bool IsAvailable
        {
            get
            {
                // If product has variants, check if any variant is in stock
                if (Variants != null && Variants.Count > 0)
                {
                    return Variants.Any(v => v.IsInStock);
                }
                
                // Otherwise, use legacy logic
                return InventoryStatus == InventoryStatus.InStock && StockQuantity > 0;
            }
        }

        /// <summary>
        /// Check if product is coming soon. If variants exist, return false (variants control availability).
        /// Otherwise, use legacy status.
        /// </summary>
        [NotMapped]
        public bool IsComingSoon
        {
            get
            {
                // If product has variants, they control availability (no "coming soon" at product level)
                if (Variants != null && Variants.Count > 0)
                    return false;
                
                // Otherwise, use legacy logic
                return InventoryStatus == InventoryStatus.ComingSoon;
            }
        }

        /// <summary>
        /// Check if product is out of stock. If variants exist, check if all are out of stock.
        /// Otherwise, use legacy status.
        /// </summary>
        [NotMapped]
        public bool IsOutOfStock
        {
            get
            {
                // If product has variants, check if all are out of stock
                if (Variants != null && Variants.Count > 0)
                {
                    return !Variants.Any(v => v.IsInStock);
                }
                
                // Otherwise, use legacy logic
                return InventoryStatus == InventoryStatus.OutOfStock || StockQuantity == 0;
            }
        }

        /// <summary>
        /// Get the product's effective inventory status based on variants or legacy status.
        /// </summary>
        [NotMapped]
        public InventoryStatus EffectiveInventoryStatus
        {
            get
            {
                // If product has variants, determine status from variants
                if (Variants != null && Variants.Count > 0)
                {
                    if (Variants.Any(v => v.IsInStock))
                        return InventoryStatus.InStock;
                    else
                        return InventoryStatus.OutOfStock;
                }
                
                // Otherwise, use legacy status
                return InventoryStatus;
            }
        }
    }
}