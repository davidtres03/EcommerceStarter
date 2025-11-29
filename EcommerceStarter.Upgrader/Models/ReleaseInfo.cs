using System;
using System.Collections.Generic;
using System.Linq;

namespace EcommerceStarter.Upgrader.Models;

/// <summary>
/// Represents information about a GitHub release
/// </summary>
public class ReleaseInfo
{
    /// <summary>
    /// Release version tag (e.g., "v1.0.0")
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Release name/title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Release description/changelog
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the release was published
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// List of assets included in the release
    /// </summary>
    public List<ReleaseAsset> Assets { get; set; } = new();

    /// <summary>
    /// Is this a pre-release?
    /// </summary>
    public bool IsPreRelease { get; set; }

    /// <summary>
    /// Is this a draft?
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// Download URL for the release (GitHub API)
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// HTML URL for viewing on GitHub
    /// </summary>
    public string HtmlUrl { get; set; } = string.Empty;

    /// <summary>
    /// Find a specific asset by name
    /// </summary>
    public ReleaseAsset? FindAsset(string assetName)
    {
        return Assets.FirstOrDefault(a => a.Name.Equals(assetName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Find asset matching a pattern (e.g., "EcommerceStarter-*.zip")
    /// </summary>
    public ReleaseAsset? FindAssetByPattern(string pattern)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        return Assets.FirstOrDefault(a => regex.IsMatch(a.Name));
    }
}
