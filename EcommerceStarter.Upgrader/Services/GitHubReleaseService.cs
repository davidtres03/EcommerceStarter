using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EcommerceStarter.Upgrader.Models;

namespace EcommerceStarter.Upgrader.Services;

/// <summary>
/// Service for downloading application from GitHub releases
/// Queries GitHub API for latest/specific releases and downloads assets
/// </summary>
public class GitHubReleaseService
{
    private readonly HttpClient _httpClient;
    private readonly string? _authToken;
    private const string RepoOwner = "davidtres03";
    private const string RepoName = "EcommerceStarter";
    private const string GitHubApiBase = "https://api.github.com";

    // Rate limit headers
    private int _rateLimitRemaining = 60;
    private DateTime _rateLimitReset = DateTime.UtcNow;

    public GitHubReleaseService(HttpClient? httpClient = null, string? authToken = null)
    {
        // Always use the shared HttpClient factory instead of creating instances
        _httpClient = HttpClientFactory.GetHttpClient();

        // Use provided token, or fall back to Credential Manager (for automatic testing support)
        // NOTE: Remove before production release
        var token = authToken ?? CredentialManagerService.GetGitHubToken();

        // NOTE: Authorization header will be added per-request to avoid thread-safety issues
        // with multiple services modifying shared HttpClient.DefaultRequestHeaders
        // Store token for use in GetLatestReleaseAsync and other methods
        _authToken = token;

        // NOTE: Timeout is set in HttpClientFactory initialization
        // Do NOT modify Timeout here as it causes InvalidOperationException if the client
        // has already been used (e.g., during reconfiguration mode)
    }

    /// <summary>
    /// Get the latest release from GitHub
    /// Falls back to ListReleasesAsync if /releases/latest endpoint fails
    /// </summary>
    public async Task<ReleaseInfo?> GetLatestReleaseAsync()
    {
        try
        {
            // Add cache-busting parameter with current timestamp to force fresh data
            var cacheBuster = DateTime.UtcNow.Ticks;
            var url = $"{GitHubApiBase}/repos/{RepoOwner}/{RepoName}/releases/latest?t={cacheBuster}";

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            // Add Authorization header per-request (thread-safe with shared HttpClient)
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
            }

            var response = await _httpClient.SendAsync(request);

            // Check rate limit headers
            UpdateRateLimit(response);

            if (!response.IsSuccessStatusCode)
            {
                // If /releases/latest fails (404, 403, etc), try listing all releases and get the first
                System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] /releases/latest failed ({response.StatusCode}), falling back to ListReleasesAsync...");

                try
                {
                    var allReleases = await ListReleasesAsync(perPage: 1, page: 1);
                    if (allReleases.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Fallback successful, using first release: {allReleases[0].Version}");
                        return allReleases[0];
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Fallback also failed: {fallbackEx.Message}");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                throw new Exception($"GitHub API error: {response.StatusCode} - {response.ReasonPhrase}");
            }

            // Read content with proper UTF-8 decoding for emoji/special characters
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var content = System.Text.Encoding.UTF8.GetString(bytes);
            return ParseReleaseJson(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get latest release from GitHub: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get a specific release by version tag
    /// </summary>
    public async Task<ReleaseInfo?> GetReleaseByVersionAsync(string versionTag)
    {
        try
        {
            // Ensure tag starts with 'v'
            if (!versionTag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                versionTag = "v" + versionTag;

            var url = $"{GitHubApiBase}/repos/{RepoOwner}/{RepoName}/releases/tags/{versionTag}";

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            // Add Authorization header per-request (thread-safe with shared HttpClient)
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
            }

            var response = await _httpClient.SendAsync(request);

            UpdateRateLimit(response);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            // Read content with proper UTF-8 decoding for emoji/special characters
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var content = System.Text.Encoding.UTF8.GetString(bytes);
            return ParseReleaseJson(content);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get release {versionTag} from GitHub: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// List all releases from GitHub
    /// </summary>
    public async Task<List<ReleaseInfo>> ListReleasesAsync(int perPage = 20, int page = 1)
    {
        try
        {
            var releases = new List<ReleaseInfo>();
            var url = $"{GitHubApiBase}/repos/{RepoOwner}/{RepoName}/releases?per_page={perPage}&page={page}";

            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            // Add Authorization header per-request (thread-safe with shared HttpClient)
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
            }

            var response = await _httpClient.SendAsync(request);

            UpdateRateLimit(response);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"GitHub API error: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var release = ParseReleaseElement(element);
                if (release != null)
                    releases.Add(release);
            }

            return releases;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to list releases from GitHub: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Download an asset from a release
    /// Reports progress via IProgress callback
    /// For private repos, uses GitHub API endpoint if browser_download_url fails
    /// </summary>
    public async Task<byte[]> DownloadAssetAsync(
        string downloadUrl,
        long? assetId = null,
        IProgress<DownloadProgress>? progress = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Attempting download from: {downloadUrl}");

            // Try browser_download_url first
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(downloadUrl));

            // Add Authorization header per-request (might be needed for private repos)
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Download failed: {response.StatusCode} - {response.ReasonPhrase}");
                response.Dispose();

                // For private repos, try GitHub API endpoint as fallback
                if (assetId.HasValue && assetId.Value > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Trying GitHub API endpoint with asset ID {assetId}...");
                    var apiUrl = $"{GitHubApiBase}/repos/{RepoOwner}/{RepoName}/releases/assets/{assetId}";
                    System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] API download URL: {apiUrl}");

                    // Create a request with proper Accept header for binary download
                    var apiRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(apiUrl));
                    apiRequest.Headers.Add("Accept", "application/octet-stream");

                    // Add Authorization header per-request
                    if (!string.IsNullOrWhiteSpace(_authToken))
                    {
                        apiRequest.Headers.Add("Authorization", $"Bearer {_authToken}");
                    }

                    response = await _httpClient.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] API endpoint also failed: {response.StatusCode}");
                        response.Dispose();
                        throw new Exception($"Download failed: API endpoint returned {response.StatusCode}");
                    }

                    System.Diagnostics.Debug.WriteLine("[GitHubReleaseService] API endpoint download succeeded");
                }
                else
                {
                    throw new Exception($"Download failed: browser download returned {response.StatusCode}");
                }
            }

            using (response)
            {
                var totalBytes = response.Content.Headers.ContentLength ?? 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;
                var downloadedBytes = 0L;
                var startTime = DateTime.UtcNow;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var memoryStream = new MemoryStream();

                while (isMoreToRead)
                {
                    var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        isMoreToRead = false;
                        continue;
                    }

                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    // Report progress
                    if (progress != null)
                    {
                        var elapsed = DateTime.UtcNow - startTime;
                        var speedMBps = elapsed.TotalSeconds > 0
                            ? (downloadedBytes / (1024.0 * 1024.0)) / elapsed.TotalSeconds
                            : 0;

                        var eta = speedMBps > 0
                            ? TimeSpan.FromSeconds((totalBytes - downloadedBytes) / (speedMBps * 1024.0 * 1024.0))
                            : TimeSpan.Zero;

                        progress.Report(new DownloadProgress
                        {
                            BytesReceived = downloadedBytes,
                            TotalBytes = totalBytes,
                            SpeedMBps = speedMBps,
                            ETA = eta,
                            ElapsedTime = elapsed
                        });
                    }
                }

                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download asset: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Verify checksum of downloaded data
    /// </summary>
    public static bool VerifyChecksum(byte[] data, string expectedHash, string algorithm = "SHA256")
    {
        try
        {
            using var hasher = System.Security.Cryptography.HashAlgorithm.Create(algorithm);
            if (hasher == null)
                throw new Exception($"Unsupported hash algorithm: {algorithm}");

            var hash = hasher.ComputeHash(data);
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            return hashString.Equals(expectedHash.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            throw new Exception($"Checksum verification failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Check if we have rate limit available
    /// </summary>
    public bool HasRateLimitAvailable()
    {
        if (DateTime.UtcNow >= _rateLimitReset)
        {
            _rateLimitRemaining = 60; // Reset to default if expired
        }

        return _rateLimitRemaining > 0;
    }

    /// <summary>
    /// Get remaining rate limit calls
    /// </summary>
    public int GetRateLimitRemaining() => _rateLimitRemaining;

    /// <summary>
    /// Get when rate limit resets
    /// </summary>
    public DateTime GetRateLimitReset() => _rateLimitReset;

    // --- Private Helpers ---

    private void UpdateRateLimit(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining))
        {
            if (int.TryParse(remaining.FirstOrDefault(), out var count))
                _rateLimitRemaining = count;
        }

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset))
        {
            if (long.TryParse(reset.FirstOrDefault(), out var unixTime))
                _rateLimitReset = UnixTimeStampToDateTime(unixTime);
        }
    }

    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
        return dateTime;
    }

    private ReleaseInfo? ParseReleaseJson(string json)
    {
        var doc = JsonDocument.Parse(json);
        return ParseReleaseElement(doc.RootElement);
    }

    private ReleaseInfo? ParseReleaseElement(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("tag_name", out var tagElement))
                return null;

            var tagName = tagElement.GetString() ?? "unknown";
            System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Parsed release tag_name: {tagName}");

            var release = new ReleaseInfo
            {
                Version = tagName,
                Name = element.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                Description = element.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "",
                IsPreRelease = element.TryGetProperty("prerelease", out var preEl) && preEl.GetBoolean(),
                IsDraft = element.TryGetProperty("draft", out var draftEl) && draftEl.GetBoolean(),
                HtmlUrl = element.TryGetProperty("html_url", out var htmlEl) ? htmlEl.GetString() ?? "" : "",
                ApiUrl = element.TryGetProperty("url", out var apiEl) ? apiEl.GetString() ?? "" : ""
            };

            if (element.TryGetProperty("published_at", out var pubEl))
            {
                if (DateTime.TryParse(pubEl.GetString(), out var pubDate))
                    release.PublishedAt = pubDate;
            }

            // Parse assets
            if (element.TryGetProperty("assets", out var assetsElement))
            {
                System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Found {assetsElement.GetArrayLength()} assets in release {tagName}");
                foreach (var assetEl in assetsElement.EnumerateArray())
                {
                    // Log raw JSON for this asset
                    var rawAssetJson = assetEl.GetRawText();
                    System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Asset for {tagName}: {rawAssetJson}");

                    var asset = ParseAssetElement(assetEl);
                    if (asset != null)
                        release.Assets.Add(asset);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] ✅ Successfully parsed release: {tagName}");
            return release;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing release: {ex.Message}");
            return null;
        }
    }

    private ReleaseAsset? ParseAssetElement(JsonElement element)
    {
        try
        {
            if (!element.TryGetProperty("name", out var nameEl))
                return null;

            var assetId = element.TryGetProperty("id", out var idEl)
                ? idEl.GetInt64()
                : 0;

            var asset = new ReleaseAsset
            {
                Id = assetId,
                Name = nameEl.GetString() ?? "unknown",
                BrowserDownloadUrl = element.TryGetProperty("browser_download_url", out var urlEl)
                    ? urlEl.GetString() ?? ""
                    : "",
                ContentType = element.TryGetProperty("content_type", out var ctEl)
                    ? ctEl.GetString() ?? ""
                    : "",
                Size = element.TryGetProperty("size", out var sizeEl)
                    ? sizeEl.GetInt64()
                    : 0
            };

            System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Parsed asset: {asset.Name} (ID={assetId}, Size={asset.Size})");
            System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] Asset download URL: {asset.BrowserDownloadUrl}");
            System.Diagnostics.Debug.WriteLine($"[GitHubReleaseService] URL is null/empty: {string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl)}");

            if (element.TryGetProperty("created_at", out var createdEl))
            {
                if (DateTime.TryParse(createdEl.GetString(), out var createdDate))
                    asset.CreatedAt = createdDate;
            }

            if (element.TryGetProperty("updated_at", out var updatedEl))
            {
                if (DateTime.TryParse(updatedEl.GetString(), out var updatedDate))
                    asset.UpdatedAt = updatedDate;
            }

            return asset;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error parsing asset: {ex.Message}");
            return null;
        }
    }
}
