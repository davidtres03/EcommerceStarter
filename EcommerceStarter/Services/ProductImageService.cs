using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Services;

/// <summary>
/// Service for managing product images with Cloudinary
/// Handles upload, deletion, and URL generation for product photos
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Upload a product image from form file
    /// </summary>
    Task<ProductImageResult> UploadProductImageAsync(IFormFile formFile, int productId = 0);

    /// <summary>
    /// Upload multiple product images at once (batch)
    /// </summary>
    Task<List<ProductImageResult>> UploadProductImagesAsync(IEnumerable<IFormFile> formFiles, int productId = 0);

    /// <summary>
    /// Delete a product image
    /// </summary>
    Task<bool> DeleteProductImageAsync(string cloudinaryPublicId);

    /// <summary>
    /// Get optimized URL for product image display
    /// </summary>
    Task<string> GetProductImageUrl(string cloudinaryPublicId, int? width = null, int? height = null);

    /// <summary>
    /// Get thumbnail URL (optimized for list views)
    /// </summary>
    Task<string> GetProductThumbnailUrl(string cloudinaryPublicId);

    /// <summary>
    /// Validate image file before upload
    /// </summary>
    (bool IsValid, string ErrorMessage) ValidateProductImage(IFormFile formFile);
}

public class ProductImageService : IProductImageService
{
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<ProductImageService> _logger;

    // Free tier considerations - we need to be mindful of usage
    private const long MaxFileSize = 5 * 1024 * 1024;  // 5 MB
    private const int MaxImagesPerBatch = 5;           // Batch limit to conserve credits
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public ProductImageService(ICloudinaryService cloudinaryService, ILogger<ProductImageService> logger)
    {
        _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductImageResult> UploadProductImageAsync(IFormFile formFile, int productId = 0)
    {
        try
        {
            // Validate image
            var (isValid, errorMessage) = ValidateProductImage(formFile);
            if (!isValid)
            {
                _logger.LogWarning($"[ProductImageService] Validation failed for product {productId}: {errorMessage}");
                return new ProductImageResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            _logger.LogInformation($"[ProductImageService] Uploading image for product {productId}: {formFile.FileName}");

            // Upload to Cloudinary
            using var stream = formFile.OpenReadStream();
            var uploadResult = await _cloudinaryService.UploadImageAsync(
                stream,
                $"product_{productId}_{formFile.FileName}",
                CloudinaryImageType.ProductImage
            );

            if (!uploadResult.Success)
            {
                _logger.LogError($"[ProductImageService] Upload failed: {uploadResult.ErrorMessage}");
                return new ProductImageResult
                {
                    Success = false,
                    ErrorMessage = uploadResult.ErrorMessage
                };
            }

            _logger.LogInformation($"[ProductImageService] Upload successful: {uploadResult.PublicId}");

            return new ProductImageResult
            {
                Success = true,
                PublicId = uploadResult.PublicId,
                Url = uploadResult.SecureUrl,
                ThumbnailUrl = await _cloudinaryService.GetOptimizedUrl(uploadResult.PublicId, CloudinaryImageType.ProductImage, 200, 200),
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format,
                FileSizeBytes = uploadResult.Bytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProductImageService] Exception uploading image: {ex.Message}");
            return new ProductImageResult
            {
                Success = false,
                ErrorMessage = $"Upload exception: {ex.Message}"
            };
        }
    }

    public async Task<List<ProductImageResult>> UploadProductImagesAsync(IEnumerable<IFormFile> formFiles, int productId = 0)
    {
        var results = new List<ProductImageResult>();

        try
        {
            var fileList = new List<IFormFile>(formFiles);

            if (fileList.Count > MaxImagesPerBatch)
            {
                _logger.LogWarning($"[ProductImageService] Batch size {fileList.Count} exceeds limit of {MaxImagesPerBatch}");
                return new List<ProductImageResult>
                {
                    new ProductImageResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot upload more than {MaxImagesPerBatch} images at once (free tier limit)"
                    }
                };
            }

            _logger.LogInformation($"[ProductImageService] Uploading {fileList.Count} images for product {productId}");

            foreach (var formFile in fileList)
            {
                var result = await UploadProductImageAsync(formFile, productId);
                results.Add(result);

                // Log progress
                if (result.Success)
                {
                    _logger.LogInformation($"[ProductImageService] Successfully uploaded: {formFile.FileName}");
                }
                else
                {
                    _logger.LogWarning($"[ProductImageService] Failed to upload: {formFile.FileName} - {result.ErrorMessage}");
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProductImageService] Exception in batch upload: {ex.Message}");
            return new List<ProductImageResult>
            {
                new ProductImageResult
                {
                    Success = false,
                    ErrorMessage = $"Batch upload exception: {ex.Message}"
                }
            };
        }
    }

    public async Task<bool> DeleteProductImageAsync(string cloudinaryPublicId)
    {
        try
        {
            if (string.IsNullOrEmpty(cloudinaryPublicId))
            {
                _logger.LogWarning("[ProductImageService] Cannot delete - empty public ID");
                return false;
            }

            _logger.LogInformation($"[ProductImageService] Deleting image: {cloudinaryPublicId}");

            var result = await _cloudinaryService.DeleteImageAsync(cloudinaryPublicId);

            if (result)
            {
                _logger.LogInformation($"[ProductImageService] Image deleted successfully: {cloudinaryPublicId}");
            }
            else
            {
                _logger.LogWarning($"[ProductImageService] Failed to delete image: {cloudinaryPublicId}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProductImageService] Exception deleting image: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetProductImageUrl(string cloudinaryPublicId, int? width = null, int? height = null)
    {
        if (string.IsNullOrEmpty(cloudinaryPublicId))
            return string.Empty;

        try
        {
            return await _cloudinaryService.GetOptimizedUrl(
                cloudinaryPublicId,
                CloudinaryImageType.ProductImage,
                width,
                height
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"[ProductImageService] Exception building URL: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<string> GetProductThumbnailUrl(string cloudinaryPublicId)
    {
        if (string.IsNullOrEmpty(cloudinaryPublicId))
            return string.Empty;

        return await GetProductImageUrl(cloudinaryPublicId, 200, 200);
    }

    public (bool IsValid, string ErrorMessage) ValidateProductImage(IFormFile formFile)
    {
        // Check if file exists
        if (formFile == null || formFile.Length == 0)
        {
            return (false, "No file selected");
        }

        // Check file size (5 MB max)
        if (formFile.Length > MaxFileSize)
        {
            return (false, $"File size exceeds maximum of {MaxFileSize / (1024 * 1024)} MB");
        }

        // Check file extension
        var ext = Path.GetExtension(formFile.FileName).ToLowerInvariant();
        if (!Array.Exists(AllowedExtensions, element => element == ext))
        {
            return (false, $"File type '{ext}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }

        // Check MIME type
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!Array.Exists(allowedMimeTypes, element => element == formFile.ContentType?.ToLowerInvariant()))
        {
            return (false, $"Invalid file MIME type: {formFile.ContentType}");
        }

        return (true, string.Empty);
    }
}

/// <summary>
/// Result of product image upload operation
/// </summary>
public class ProductImageResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;       // Cloudinary public ID for later deletion/updates
    public string Url { get; set; } = string.Empty;            // Full-size image URL
    public string ThumbnailUrl { get; set; } = string.Empty;   // Thumbnail URL (200x200)
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
