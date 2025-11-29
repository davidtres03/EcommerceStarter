using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Services;

/// <summary>
/// Service for managing image uploads and optimization via Cloudinary
/// Handles product images, user avatars, and general media with automatic enhancement
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Upload an image and get optimized URL with transformations
    /// </summary>
    Task<CloudinaryUploadResult> UploadImageAsync(Stream imageStream, string fileName, CloudinaryImageType imageType);

    /// <summary>
    /// Upload image from URL (for batch processing)
    /// </summary>
    Task<CloudinaryUploadResult> UploadImageFromUrlAsync(string imageUrl, string fileName, CloudinaryImageType imageType);

    /// <summary>
    /// Delete an image from Cloudinary
    /// </summary>
    Task<bool> DeleteImageAsync(string publicId);

    /// <summary>
    /// Get optimized URL for an image with optional transformations
    /// </summary>
    Task<string> GetOptimizedUrl(string publicId, CloudinaryImageType imageType, int? width = null, int? height = null);

    /// <summary>
    /// Get current usage statistics for free tier monitoring
    /// </summary>
    Task<CloudinaryUsageStats> GetUsageStatsAsync();
}

public class CloudinaryService : ICloudinaryService
{
    private Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly IApiConfigurationService _apiConfigService;
    private string? _cloudName;
    private bool _initialized = false;

    public CloudinaryService(ILogger<CloudinaryService> logger, IApiConfigurationService apiConfigService)
    {
        _logger = logger;
        _apiConfigService = apiConfigService;
    }

    private async Task<bool> EnsureInitializedAsync()
    {
        if (_initialized && _cloudinary != null)
            return true;

        try
        {
            // Load credentials from database (ApiConfigurations table)
            var configs = await _apiConfigService.GetConfigurationsByTypeAsync("Cloudinary", activeOnly: true);
            var config = configs.OrderBy(c => c.Id).FirstOrDefault();

            if (config == null)
            {
                _logger.LogWarning("[Cloudinary] No active Cloudinary configuration found in database");
                return false;
            }

            // Decrypt the values using ApiConfigurationService
            var decryptedValues = await _apiConfigService.GetDecryptedValuesAsync(config.Id);
            var cloudName = decryptedValues["Value1"];  // CloudName
            var apiKey = decryptedValues["Value2"];     // ApiKey
            var apiSecret = decryptedValues["Value3"];  // ApiSecret

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                _logger.LogWarning("[Cloudinary] Cloudinary credentials incomplete. Please configure in Admin > API Configurations");
                return false;
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudName = cloudName;
            _initialized = true;

            _logger.LogInformation("[Cloudinary] Successfully initialized with database credentials");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Failed to initialize from database");
            return false;
        }
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(Stream imageStream, string fileName, CloudinaryImageType imageType)
    {
        try
        {
            if (!await EnsureInitializedAsync() || _cloudinary == null)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = "Cloudinary not configured. Please set up Cloudinary credentials in Admin > Settings > API Configurations"
                };
            }

            _logger.LogInformation("[Cloudinary] Uploading image: {FileName} (Type: {ImageType})", fileName, imageType);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, imageStream),
                PublicId = GeneratePublicId(fileName, imageType),
                Folder = GetFolderForImageType(imageType),
                Tags = $"{imageType.ToString()},ecommerce",
                Context = new StringDictionary
                {
                    { "alt", fileName },
                    { "type", imageType.ToString() }
                }
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("[Cloudinary] Upload failed: {Error}", uploadResult.Error.Message);
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = uploadResult.Error.Message
                };
            }

            _logger.LogInformation("[Cloudinary] Upload successful: {PublicId}", uploadResult.PublicId);

            return new CloudinaryUploadResult
            {
                Success = true,
                PublicId = uploadResult.PublicId,
                SecureUrl = uploadResult.SecureUrl.AbsoluteUri,
                CloudinaryUrl = uploadResult.Url.AbsoluteUri,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format,
                Bytes = uploadResult.Bytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Exception during upload");
            return new CloudinaryUploadResult
            {
                Success = false,
                ErrorMessage = $"Upload exception: {ex.Message}"
            };
        }
    }

    public async Task<CloudinaryUploadResult> UploadImageFromUrlAsync(string imageUrl, string fileName, CloudinaryImageType imageType)
    {
        try
        {
            if (!await EnsureInitializedAsync() || _cloudinary == null)
            {
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = "Cloudinary not configured. Please set up Cloudinary credentials in Admin > Settings > API Configurations"
                };
            }

            _logger.LogInformation("[Cloudinary] Uploading from URL: {FileName}", fileName);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageUrl),
                PublicId = GeneratePublicId(fileName, imageType),
                Folder = GetFolderForImageType(imageType),
                Tags = $"{imageType.ToString()},ecommerce",
                Context = new StringDictionary
                {
                    { "alt", fileName },
                    { "type", imageType.ToString() }
                }
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("[Cloudinary] URL upload failed: {Error}", uploadResult.Error.Message);
                return new CloudinaryUploadResult
                {
                    Success = false,
                    ErrorMessage = uploadResult.Error.Message
                };
            }

            return new CloudinaryUploadResult
            {
                Success = true,
                PublicId = uploadResult.PublicId,
                SecureUrl = uploadResult.SecureUrl.AbsoluteUri,
                CloudinaryUrl = uploadResult.Url.AbsoluteUri,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Format = uploadResult.Format,
                Bytes = uploadResult.Bytes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Exception during URL upload");
            return new CloudinaryUploadResult
            {
                Success = false,
                ErrorMessage = $"Upload exception: {ex.Message}"
            };
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        try
        {
            if (!await EnsureInitializedAsync() || _cloudinary == null)
            {
                _logger.LogWarning("[Cloudinary] Cannot delete - Cloudinary not configured");
                return false;
            }

            _logger.LogInformation($"[Cloudinary] Deleting image: {publicId}");

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
            {
                _logger.LogWarning($"[Cloudinary] Delete failed: {result.Error.Message}");
                return false;
            }

            _logger.LogInformation($"[Cloudinary] Image deleted successfully: {publicId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Cloudinary] Exception during delete: {ex.Message}");
            return false;
        }
    }

    public async Task<string> GetOptimizedUrl(string publicId, CloudinaryImageType imageType, int? width = null, int? height = null)
    {
        try
        {
            // For URL generation, we need the cloud name
            if (string.IsNullOrEmpty(_cloudName))
            {
                // Try to initialize if not already done
                await EnsureInitializedAsync();
            }

            if (string.IsNullOrEmpty(_cloudName))
                return string.Empty;

            var transformation = GetTransformationForImageType(imageType, width, height);
            
            // Build URL using the transformation directly
            // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/image/upload/{transformations}/{public_id}
            var transformationString = transformation.ToString();
            var baseUrl = $"https://res.cloudinary.com/{_cloudName}/image/upload";
            
            if (!string.IsNullOrEmpty(transformationString))
            {
                return $"{baseUrl}/{transformationString}/{publicId}";
            }
            
            return $"{baseUrl}/{publicId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Exception building URL");
            return string.Empty;
        }
    }

    public async Task<CloudinaryUsageStats> GetUsageStatsAsync()
    {
        try
        {
            // Note: Usage stats require authenticated access to dashboard
            // This is a placeholder - actual implementation depends on Cloudinary API availability
            _logger.LogInformation("[Cloudinary] Retrieving usage statistics...");

            return new CloudinaryUsageStats
            {
                LastUpdated = DateTime.UtcNow,
                Note = "Check Cloudinary dashboard for actual usage: https://cloudinary.com/console"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cloudinary] Exception getting usage stats");
            return new CloudinaryUsageStats { Error = ex.Message };
        }
    }

    /// <summary>
    /// Generate consistent public ID for image organization
    /// </summary>
    private static string GeneratePublicId(string fileName, CloudinaryImageType imageType)
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var sanitized = System.Text.RegularExpressions.Regex.Replace(name, "[^a-zA-Z0-9_-]", "_");

        return $"{imageType.ToString().ToLower()}_{sanitized}_{timestamp}";
    }

    /// <summary>
    /// Get folder path based on image type
    /// </summary>
    private static string GetFolderForImageType(CloudinaryImageType imageType) => imageType switch
    {
        CloudinaryImageType.ProductImage => "products",
        CloudinaryImageType.UserAvatar => "avatars",
        CloudinaryImageType.CompanyLogo => "branding",
        CloudinaryImageType.Banner => "banners",
        _ => "general"
    };

    /// <summary>
    /// Get default transformation based on image type
    /// Applies AI-powered enhancements: auto-enhance, shadow removal, and optimization
    /// </summary>
    private static Transformation GetTransformationForImageType(CloudinaryImageType imageType, int? customWidth = null, int? customHeight = null)
    {
        var baseTransformation = new Transformation()
            .Quality("auto:best")       // Best quality with smart compression
            .FetchFormat("auto");       // Auto format (webp for modern browsers)

        return imageType switch
        {
            CloudinaryImageType.ProductImage => baseTransformation
                .Width(customWidth ?? 800).Height(customHeight ?? 800).Crop("fill").Gravity("auto")
                .Effect("improve")           // AI-powered auto-enhancement (contrast, saturation, brightness)
                .Effect("auto_contrast")     // Auto contrast adjustment
                .Effect("auto_color")        // Auto color correction
                .Effect("shadow:40")         // Shadow fill/removal effect
                .Effect("sharpen:100")       // Sharpen for crisp product details
                .Dpr("auto"),                // Auto device pixel ratio for retina displays

            CloudinaryImageType.UserAvatar => baseTransformation
                .Width(customWidth ?? 200).Height(customHeight ?? 200).Crop("thumb").Gravity("face")
                .Effect("improve")           // AI enhancement for faces
                .Effect("auto_color")
                .Radius("max")               // Circle avatars
                .Dpr("auto"),

            CloudinaryImageType.CompanyLogo => baseTransformation
                .Width(customWidth ?? 300).Height(customHeight ?? 300).Crop("fit")
                .Effect("improve")
                .Effect("auto_contrast")
                .Background("transparent")   // Preserve transparency
                .Dpr("auto"),

            CloudinaryImageType.Banner => baseTransformation
                .Width(customWidth ?? 1200).Height(customHeight ?? 400).Crop("fill").Gravity("auto")
                .Effect("improve")           // AI enhancement
                .Effect("auto_contrast")
                .Effect("auto_color")
                .Effect("sharpen:50")
                .Quality("auto:good")        // Balanced quality for banners
                .Dpr("auto"),

            _ => baseTransformation
                .Width(customWidth ?? 500).Height(customHeight ?? 500).Crop("fit")
                .Effect("improve")           // Apply AI enhancement to all images
                .Dpr("auto")
        };
    }
}

/// <summary>
/// Configuration options for Cloudinary
/// </summary>
public class CloudinaryOptions
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

/// <summary>
/// Image type enumeration for organization and transformation
/// </summary>
public enum CloudinaryImageType
{
    ProductImage,
    UserAvatar,
    CompanyLogo,
    Banner,
    General
}

/// <summary>
/// Result of image upload operation
/// </summary>
public class CloudinaryUploadResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string CloudinaryUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = string.Empty;
    public long Bytes { get; set; }
}

/// <summary>
/// Cloudinary usage statistics
/// </summary>
public class CloudinaryUsageStats
{
    public DateTime LastUpdated { get; set; }
    public string Note { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
