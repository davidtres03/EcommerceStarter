using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Implementation of image upload service
    /// Handles logo and favicon uploads with validation and metadata stripping
    /// </summary>
    public class ImageUploadService : IImageUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageUploadService> _logger;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico" };
        private static readonly string[] AllowedMimeTypes = { 
            "image/jpeg", "image/png", "image/gif", "image/svg+xml", "image/x-icon", "image/vnd.microsoft.icon" 
        };

        public long MaxFileSizeBytes => 5 * 1024 * 1024; // 5MB

        public ImageUploadService(
            IWebHostEnvironment environment,
            ILogger<ImageUploadService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            // Check file size
            if (file.Length > MaxFileSizeBytes)
            {
                _logger.LogWarning("File size {Size} exceeds maximum {Max}", file.Length, MaxFileSizeBytes);
                return false;
            }

            // Check extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("File extension {Extension} not allowed", extension);
                return false;
            }

            // Check MIME type
            if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("MIME type {MimeType} not allowed", file.ContentType);
                return false;
            }

            // Validate file content (magic bytes) for non-SVG files
            // SVG files are XML-based and don't have magic bytes
            if (extension != ".svg" && extension != ".ico")
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    var header = new byte[8];
                    stream.Read(header, 0, Math.Min(8, (int)stream.Length));
                    
                    // Check for known image signatures
                    var isJpeg = header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
                    var isPng = header.Length >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
                    var isGif = header.Length >= 3 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46;
                    
                    if (!isJpeg && !isPng && !isGif)
                    {
                        _logger.LogWarning("File content does not match expected image format (magic bytes validation failed)");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating file content");
                    return false;
                }
            }

            return true;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder = "uploads")
        {
            if (!IsValidImage(file))
            {
                throw new InvalidOperationException("Invalid image file");
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, folder);
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Created uploads directory: {Path}", uploadsPath);
                }

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // For images that support metadata (JPEG, PNG), strip it
                // SVG and ICO files are saved as-is
                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
                {
                    await StripMetadataAndSaveAsync(file, filePath);
                }
                else
                {
                    // Save file as-is for SVG, ICO, GIF
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                // Return relative URL
                var relativeUrl = $"/{folder}/{fileName}";
                _logger.LogInformation("Image uploaded successfully: {Url}", relativeUrl);

                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                throw;
            }
        }

        /// <summary>
        /// Strips all EXIF metadata from an image and saves it
        /// This removes GPS location, camera info, software info, timestamps, etc.
        /// </summary>
        private async Task StripMetadataAndSaveAsync(IFormFile file, string outputPath)
        {
            try
            {
                using var inputStream = file.OpenReadStream();
                using var image = await Image.LoadAsync(inputStream);

                // Remove all EXIF metadata
                image.Metadata.ExifProfile = null;
                
                // Remove IPTC metadata (copyright, keywords, etc.)
                image.Metadata.IptcProfile = null;
                
                // Remove XMP metadata (Adobe metadata)
                image.Metadata.XmpProfile = null;
                
                // Remove ICC color profile to reduce file size
                // Note: This may slightly change colors in rare cases
                image.Metadata.IccProfile = null;

                // Determine format and save with appropriate settings
                var extension = Path.GetExtension(outputPath).ToLowerInvariant();
                
                if (extension == ".jpg" || extension == ".jpeg")
                {
                    var encoder = new JpegEncoder
                    {
                        Quality = 90, // High quality
                        SkipMetadata = true // Extra safety: skip all metadata
                    };
                    await image.SaveAsJpegAsync(outputPath, encoder);
                    _logger.LogInformation("Stripped EXIF metadata from JPEG: {Path}", outputPath);
                }
                else if (extension == ".png")
                {
                    var encoder = new PngEncoder
                    {
                        SkipMetadata = true // Extra safety: skip all metadata
                    };
                    await image.SaveAsPngAsync(outputPath, encoder);
                    _logger.LogInformation("Stripped metadata from PNG: {Path}", outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stripping metadata from image");
                // Fallback: save the original file without metadata stripping
                using (var stream = new FileStream(outputPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogWarning("Saved image without metadata stripping due to error");
            }
        }

        public async Task<bool> DeleteImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return false;

                // Convert URL to physical path
                var relativePath = imageUrl.TrimStart('/');
                var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);

                if (File.Exists(physicalPath))
                {
                    await Task.Run(() => File.Delete(physicalPath));
                    _logger.LogInformation("Image deleted successfully: {Path}", imageUrl);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image: {Url}", imageUrl);
                return false;
            }
        }
    }
}
