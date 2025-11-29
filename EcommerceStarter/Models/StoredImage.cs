using System.ComponentModel.DataAnnotations;

namespace EcommerceStarter.Models
{
    /// <summary>
    /// Unified image storage for both local (base64) and cloud (URL) images
    /// All image data is encrypted at rest for security
    /// </summary>
    public class StoredImage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// Storage type: "local" (base64 data) or "cloudinary" (URL)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string StorageType { get; set; } = "local"; // "local" or "cloudinary"

        /// <summary>
        /// Encrypted base64 image data (for local storage) OR encrypted Cloudinary URL
        /// </summary>
        [Required]
        public string EncryptedData { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [StringLength(50)]
        public string? Category { get; set; } // "branding", "products", etc.

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? UploadedBy { get; set; }

        /// <summary>
        /// Reference to which entity uses this image (e.g., "Product:123", "SiteSettings:Logo")
        /// </summary>
        [StringLength(500)]
        public string? UsedBy { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
