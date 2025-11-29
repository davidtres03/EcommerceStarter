using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for managing encrypted images in the database
    /// Supports both local storage (base64) and cloud storage (Cloudinary URLs)
    /// </summary>
    public interface IStoredImageService
    {
        Task<Guid> SaveLocalImageAsync(IFormFile file, string category, string usedBy, string? uploadedBy = null);
        Task<Guid> SaveCloudinaryUrlAsync(string url, string fileName, string contentType, long fileSize, string category, string usedBy, string? uploadedBy = null);
        Task<StoredImage?> GetImageAsync(Guid id);
        Task<string> GetDecryptedDataAsync(Guid id);
        Task<string> GetImageAsBase64DataUriAsync(Guid id);
        Task<bool> DeleteImageAsync(Guid id);
    }

    public class StoredImageService : IStoredImageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<StoredImageService> _logger;

        public StoredImageService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            ILogger<StoredImageService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        /// <summary>
        /// Saves a local image file as encrypted base64 in the database
        /// </summary>
        public async Task<Guid> SaveLocalImageAsync(IFormFile file, string category, string usedBy, string? uploadedBy = null)
        {
            try
            {
                // Convert to base64
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(imageBytes);
                var mimeType = file.ContentType;
                
                var base64DataUri = $"data:{mimeType};base64,{base64}";

                // Encrypt the base64 data URI
                var encryptedData = _encryptionService.Encrypt(base64DataUri);

                var storedImage = new StoredImage
                {
                    Id = Guid.NewGuid(),
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    StorageType = "local",
                    EncryptedData = encryptedData,
                    FileSize = file.Length,
                    Category = category,
                    UsedBy = usedBy,
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.StoredImages.Add(storedImage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved local image {FileName} ({Size} bytes) to StoredImages with ID {Id} (encrypted)", 
                    file.FileName, file.Length, storedImage.Id);

                return storedImage.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save local image {FileName}", file.FileName);
                throw;
            }
        }

        /// <summary>
        /// Saves a Cloudinary URL as encrypted string in the database
        /// </summary>
        public async Task<Guid> SaveCloudinaryUrlAsync(string url, string fileName, string contentType, long fileSize, string category, string usedBy, string? uploadedBy = null)
        {
            try
            {
                // Encrypt the Cloudinary URL
                var encryptedUrl = _encryptionService.Encrypt(url);

                var storedImage = new StoredImage
                {
                    Id = Guid.NewGuid(),
                    FileName = fileName,
                    ContentType = contentType,
                    StorageType = "cloudinary",
                    EncryptedData = encryptedUrl,
                    FileSize = fileSize,
                    Category = category,
                    UsedBy = usedBy,
                    UploadedBy = uploadedBy,
                    UploadedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                _context.StoredImages.Add(storedImage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved Cloudinary URL for {FileName} to StoredImages with ID {Id} (encrypted)", 
                    fileName, storedImage.Id);

                return storedImage.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Cloudinary URL for {FileName}", fileName);
                throw;
            }
        }

        /// <summary>
        /// Gets an image from the database (encrypted)
        /// </summary>
        public async Task<StoredImage?> GetImageAsync(Guid id)
        {
            return await _context.StoredImages
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        }

        /// <summary>
        /// Gets decrypted data (base64 data URI for local, URL for Cloudinary)
        /// </summary>
        public async Task<string> GetDecryptedDataAsync(Guid id)
        {
            try
            {
                var image = await GetImageAsync(id);
                if (image == null)
                {
                    _logger.LogWarning("Image {Id} not found for decryption", id);
                    return string.Empty;
                }

                // Decrypt the data
                var decryptedData = _encryptionService.Decrypt(image.EncryptedData);

                _logger.LogDebug("Decrypted image {Id} (type: {Type})", id, image.StorageType);

                return decryptedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt image {Id}", id);
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets an image as a base64 data URI for embedding in HTML/emails
        /// For Cloudinary images, returns the URL directly (emails can fetch it)
        /// For local images, returns the decrypted base64 data URI
        /// </summary>
        public async Task<string> GetImageAsBase64DataUriAsync(Guid id)
        {
            try
            {
                var image = await GetImageAsync(id);
                if (image == null)
                {
                    _logger.LogWarning("Image {Id} not found for base64 conversion", id);
                    return string.Empty;
                }

                var decryptedData = _encryptionService.Decrypt(image.EncryptedData);

                if (image.StorageType == "local")
                {
                    // Already a base64 data URI
                    _logger.LogDebug("Returning local image {Id} as base64 data URI", id);
                    return decryptedData;
                }
                else // cloudinary
                {
                    _logger.LogDebug("Returning Cloudinary URL for image {Id}", id);
                    return decryptedData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get base64 data URI for image {Id}", id);
                return string.Empty;
            }
        }

        /// <summary>
        /// Soft deletes an image
        /// </summary>
        public async Task<bool> DeleteImageAsync(Guid id)
        {
            try
            {
                var image = await _context.StoredImages.FindAsync(id);
                if (image == null)
                {
                    _logger.LogWarning("Image {Id} not found for deletion", id);
                    return false;
                }

                image.IsDeleted = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Soft deleted image {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image {Id}", id);
                return false;
            }
        }
    }
}
