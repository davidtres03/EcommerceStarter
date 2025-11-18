using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Represents a specific attribute value for a variant
    /// For example: A shirt variant might have Color="Red" and Size="Medium"
    /// </summary>
    [Table("VariantAttributeValues")]
    public class VariantAttributeValue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        public ProductVariant? ProductVariant { get; set; }

        [Required]
        public int VariantAttributeId { get; set; }

        [ForeignKey("VariantAttributeId")]
        public VariantAttribute? VariantAttribute { get; set; }

        /// <summary>
        /// The value for this attribute (e.g., "Red" for Color attribute, "Medium" for Size)
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "Attribute Value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// When created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
