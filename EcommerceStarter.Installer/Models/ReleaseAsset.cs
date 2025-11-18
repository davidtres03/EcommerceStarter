using System;
using System.Text.Json.Serialization;

namespace EcommerceStarter.Installer.Models;

/// <summary>
/// Represents a single asset/file in a GitHub release
/// </summary>
public class ReleaseAsset
{
    /// <summary>
    /// GitHub asset ID - used for API downloads
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Asset name (e.g., "EcommerceStarter-v1.0.0.zip")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Download URL for the asset
    /// </summary>
    [JsonPropertyName("browser_download_url")]
    public required string BrowserDownloadUrl { get; set; }

    /// <summary>
    /// Size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Asset content type (e.g., "application/zip")
    /// </summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// When the asset was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the asset was last updated
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
