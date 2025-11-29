using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Represents a product variant (color, size, etc.)
    /// Allows a single product to have multiple variations with different images and stock levels
    /// </summary>
    [Table("ProductVariants")]
    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        /// <summary>
        /// Variant name/label (e.g., "Pearl White", "Rainbow")
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "Variant Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// SKU for this specific variant
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "SKU")]
        public string? Sku { get; set; }

        /// <summary>
        /// Stock quantity for this specific variant
        /// </summary>
        [Display(Name = "Stock Quantity")]
        [Range(0, 10000)]
        public int StockQuantity { get; set; }

        /// <summary>
        /// Image URL for this variant (different from product's main image)
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Variant Image URL")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Additional images for this variant (comma-separated URLs)
        /// </summary>
        [MaxLength(2000)]
        [Display(Name = "Additional Images")]
        public string? AdditionalImages { get; set; }

        /// <summary>
        /// Price override for this variant (if null, use product's base price)
        /// </summary>
        [Display(Name = "Price Override")]
        [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10000")]
        public decimal? PriceOverride { get; set; }

        /// <summary>
        /// Display order for this variant
        /// </summary>
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Whether this variant is currently available
        /// </summary>
        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;

        /// <summary>
        /// Whether this variant is featured on the homepage
        /// Only one variant per product can be featured at a time
        /// </summary>
        [Display(Name = "Featured Variant")]
        public bool IsFeatured { get; set; } = false;

        /// <summary>
        /// When the variant was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the variant was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Attribute values for this variant (e.g., Color: Red, Size: Medium)
        /// </summary>
        public ICollection<VariantAttributeValue> AttributeValues { get; set; } = new List<VariantAttributeValue>();

        /// <summary>
        /// Helper property to get effective price (override or product price)
        /// </summary>
        [NotMapped]
        public decimal EffectivePrice => PriceOverride ?? (Product?.Price ?? 0);

        /// <summary>
        /// Helper property to check if variant is in stock
        /// </summary>
        [NotMapped]
        public bool IsInStock => StockQuantity > 0 && IsAvailable;

        /// <summary>
        /// Get additional images as a list
        /// </summary>
        [NotMapped]
        public List<string> AdditionalImageList
        {
            get
            {
                if (string.IsNullOrEmpty(AdditionalImages))
                    return new List<string>();
                return AdditionalImages
                    .Split(',')
                    .Select(img => img.Trim())
                    .Where(img => !string.IsNullOrEmpty(img))
                    .ToList();
            }
        }

        /// <summary>
        /// Get variant display name from attributes (e.g., "Red - Medium")
        /// Falls back to Name property if no attributes
        /// </summary>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                if (AttributeValues == null || AttributeValues.Count == 0)
                    return Name;

                var attributeStrings = AttributeValues
                    .OrderBy(av => av.VariantAttribute?.DisplayOrder ?? 0)
                    .Select(av => av.Value)
                    .ToList();

                return string.Join(" - ", attributeStrings);
            }
        }
    }
}
