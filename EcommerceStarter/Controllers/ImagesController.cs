using Microsoft.AspNetCore.Mvc;
using EcommerceStarter.Services;

namespace EcommerceStarter.Controllers
{
    /// <summary>
    /// API Controller for serving stored images from the database
    /// </summary>
    [Route("images")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly IStoredImageService _storedImageService;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(
            IStoredImageService storedImageService,
            ILogger<ImagesController> logger)
        {
            _storedImageService = storedImageService;
            _logger = logger;
        }

        /// <summary>
        /// Serves an image from the database by ID
        /// GET: /images/stored/{id}
        /// </summary>
        [HttpGet("stored/{id:guid}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetStoredImage(Guid id)
        {
            try
            {
                var image = await _storedImageService.GetImageAsync(id);
                if (image == null)
                {
                    _logger.LogWarning("Image {Id} not found", id);
                    return NotFound();
                }

                // Decrypt the data
                var decryptedData = await _storedImageService.GetDecryptedDataAsync(id);
                if (string.IsNullOrEmpty(decryptedData))
                {
                    _logger.LogError("Failed to decrypt image {Id}", id);
                    return StatusCode(500, "Failed to decrypt image");
                }

                // If it's a local image (base64 data URI), extract the bytes
                if (image.StorageType == "local")
                {
                    // Format: data:image/png;base64,iVBORw0KG...
                    if (decryptedData.StartsWith("data:"))
                    {
                        var parts = decryptedData.Split(',');
                        if (parts.Length == 2)
                        {
                            var base64Data = parts[1];
                            var mimeType = parts[0].Split(';')[0].Replace("data:", "");

                            var imageBytes = Convert.FromBase64String(base64Data);
                            return File(imageBytes, mimeType);
                        }
                    }
                }
                else if (image.StorageType == "cloudinary")
                {
                    // For Cloudinary, redirect to the URL
                    return Redirect(decryptedData);
                }

                _logger.LogError("Unknown storage type or invalid data format for image {Id}", id);
                return StatusCode(500, "Invalid image format");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format for image {Id}", id);
                return BadRequest("Invalid image format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving image {Id}", id);
                return StatusCode(500, "Error serving image");
            }
        }

        /// <summary>
        /// Health check endpoint to verify the images API is working
        /// GET: /images/health
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
