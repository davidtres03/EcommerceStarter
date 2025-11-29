namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for handling image uploads and management
    /// </summary>
    public interface IImageUploadService
    {
        /// <summary>
        /// Uploads an image and returns the URL
        /// </summary>
        Task<string> UploadImageAsync(IFormFile file, string folder = "uploads");

        /// <summary>
        /// Deletes an image by URL
        /// </summary>
        Task<bool> DeleteImageAsync(string imageUrl);

        /// <summary>
        /// Validates if the file is a valid image
        /// </summary>
        bool IsValidImage(IFormFile file);

        /// <summary>
        /// Gets the maximum allowed file size in bytes
        /// </summary>
        long MaxFileSizeBytes { get; }
    }
}
