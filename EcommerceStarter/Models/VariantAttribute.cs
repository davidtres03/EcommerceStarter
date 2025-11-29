using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Represents an attribute type for product variants (e.g., "Color", "Size", "Material")
    /// Defines what attributes are available for a product
    /// </summary>
    [Table("VariantAttributes")]
    public class VariantAttribute
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        /// <summary>
        /// Attribute name (e.g., "Color", "Size", "Material")
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "Attribute Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Comma-separated values for this attribute (e.g., "Red,Blue,Green" or "XS,S,M,L,XL")
        /// </summary>
        [Required]
        [MaxLength(1000)]
        [Display(Name = "Attribute Values")]
        public string Values { get; set; } = string.Empty;

        /// <summary>
        /// Display order for this attribute
        /// </summary>
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        /// <summary>
        /// When the attribute was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the attribute was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Get values as a list
        /// </summary>
        [NotMapped]
        public List<string> ValuesList
        {
            get
            {
                if (string.IsNullOrEmpty(Values))
                    return new List<string>();
                return Values
                    .Split(',')
                    .Select(v => v.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();
            }
        }
    }
}
