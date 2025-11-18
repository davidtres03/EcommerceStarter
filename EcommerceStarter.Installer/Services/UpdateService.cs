using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for checking and applying updates from GitHub
/// </summary>
public class UpdateService
{
    private const string GITHUB_API_BASE = "https://api.github.com";
    private const string REPO_OWNER = "davidtres03";
    private const string REPO_NAME = "EcommerceStarter";

    private readonly HttpClient _httpClient;
    private readonly string? _authToken; // Temporary: For private repo access during testing

    public UpdateService(string? authToken = null)
    {
        try
        {
            DebugLogger.Log("[UpdateService] === UpdateService Constructor ===");

            // Use the shared HttpClient factory instead of creating new instances
            DebugLogger.Log("[UpdateService] Getting shared HttpClient...");
            _httpClient = HttpClientFactory.GetHttpClient();
            DebugLogger.Log("[UpdateService] HttpClient obtained");

            // Use provided token, or fall back to Credential Manager (for automatic testing support)
            // NOTE: Remove before production release
            DebugLogger.Log("[UpdateService] Getting GitHub token...");
            _authToken = authToken ?? CredentialManagerService.GetGitHubToken();
            DebugLogger.Log($"[UpdateService] Auth token obtained: {!string.IsNullOrEmpty(_authToken)}");

            // NOTE: Authorization header will be added per-request to avoid thread-safety issues
            // with multiple services modifying shared HttpClient.DefaultRequestHeaders

            DebugLogger.Log("[UpdateService] Constructor complete");
        }
        catch (InvalidOperationException invalidEx)
        {
            DebugLogger.Log("[UpdateService] InvalidOperationException in constructor");
            DebugLogger.LogException(invalidEx, "UpdateService constructor");
            throw;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"[UpdateService] Exception in constructor: {ex.GetType().Name}");
            DebugLogger.LogException(ex, "UpdateService constructor");
            throw;
        }
    }

    /// <summary>
    /// Check if there's a newer version available on GitHub
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = GetCurrentVersion();
            var latestRelease = await GetLatestReleaseAsync();

            if (latestRelease == null)
                return null;

            var latestVersion = ParseVersion(latestRelease.TagName);

            if (latestVersion > currentVersion)
            {
                return new UpdateInfo
                {
                    CurrentVersion = currentVersion.ToString(),
                    LatestVersion = latestVersion.ToString(),
                    ReleaseNotes = latestRelease.Body,
                    DownloadUrl = latestRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"))?.BrowserDownloadUrl ?? string.Empty,
                    PublishedAt = latestRelease.PublishedAt,
                    HasUpdate = true
                };
            }

            return new UpdateInfo
            {
                CurrentVersion = currentVersion.ToString(),
                LatestVersion = latestVersion.ToString(),
                HasUpdate = false
            };
        }
        catch (Exception ex)
        {
            // Update check failed - non-fatal, continue with current version
            return new UpdateInfo
            {
                CurrentVersion = GetCurrentVersion().ToString(),
                HasUpdate = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Check for application updates (not installer updates)
    /// </summary>
    public async Task<ApplicationUpdateInfo?> CheckForApplicationUpdatesAsync()
    {
        try
        {
            var latestRelease = await GetLatestReleaseAsync();

            if (latestRelease == null)
            {
                System.Diagnostics.Debug.WriteLine("[UpdateService] No latest release found");
                return null;
            }

            // Find the application zip file - prioritize Installer version with full naming convention
            // Preferred: EcommerceStarter-Installer-v*.zip (contains migrations)
            // Fallback: EcommerceStarter-*.zip (legacy naming)
            var appAsset = latestRelease.Assets.FirstOrDefault(a =>
                a.Name.StartsWith("EcommerceStarter-Installer-", StringComparison.OrdinalIgnoreCase) &&
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

            if (appAsset == null)
            {
                // Try legacy naming as fallback
                appAsset = latestRelease.Assets.FirstOrDefault(a =>
                    a.Name.StartsWith("EcommerceStarter-", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    !a.Name.Contains("Installer"));
            }

            if (appAsset == null)
            {
                System.Diagnostics.Debug.WriteLine("[UpdateService] No ZIP asset found in release");
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Available assets: {string.Join(", ", latestRelease.Assets.Select(a => a.Name))}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"[UpdateService] TagName from GitHub: '{latestRelease.TagName}'");
            var version = ParseVersion(latestRelease.TagName);
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Parsed version: {version}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Found update: {version} - {appAsset.Name} (AssetId: {appAsset.Id})");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Asset download URL: '{appAsset.BrowserDownloadUrl}'");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] URL is null/empty: {string.IsNullOrWhiteSpace(appAsset.BrowserDownloadUrl)}");

            if (string.IsNullOrWhiteSpace(appAsset.BrowserDownloadUrl))
            {
                DebugLogger.Log($"[UpdateService] ERROR: BrowserDownloadUrl is empty for asset {appAsset.Name}");
                System.Diagnostics.Debug.WriteLine("[UpdateService] ERROR: BrowserDownloadUrl is empty!");
                return null;
            }

            return new ApplicationUpdateInfo
            {
                Version = version.ToString(),
                DownloadUrl = appAsset.BrowserDownloadUrl,
                ReleaseNotes = latestRelease.Body,
                PublishedAt = latestRelease.PublishedAt,
                Size = appAsset.Size,
                AssetId = appAsset.Id
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] ERROR in CheckForApplicationUpdatesAsync: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Stack: {ex.StackTrace}");
            return null;
        }
    }

    /// <summary>
    /// Download and apply installer update
    /// For private repos, uses GitHub API endpoint if browser_download_url fails
    /// </summary>
    public async Task<bool> DownloadAndApplyInstallerUpdateAsync(string downloadUrl, IProgress<int>? progress = null, long? assetId = null)
    {
        // ?? DEMO MODE PROTECTION - Don't download in demo!
        if (App.IsDemoMode)
        {
            // Simulate download progress
            for (int i = 0; i <= 100; i += 10)
            {
                progress?.Report(i);
                await Task.Delay(200);
            }
            System.Diagnostics.Debug.WriteLine("?? DEMO: Simulated installer update (no actual download)");
            return true; // Pretend it worked
        }

        try
        {
            // Download to temp file
            var tempFile = Path.Combine(Path.GetTempPath(), $"EcommerceStarter-Installer-Update-{Guid.NewGuid()}.exe");

            // Try browser_download_url first
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(downloadUrl));

            // Add Authorization header per-request if needed (for private repos)
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
            }

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                // For private repos, try GitHub API endpoint as fallback
                if (assetId.HasValue && assetId.Value > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateService] Browser URL failed, trying API endpoint with asset ID {assetId}...");
                    response.Dispose();

                    var apiUrl = $"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/assets/{assetId}";
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
                        System.Diagnostics.Debug.WriteLine($"[UpdateService] API endpoint also failed: {response.StatusCode}");
                        response.Dispose();
                        throw new Exception($"Download failed: {response.StatusCode}");
                    }
                }
                else
                {
                    response.Dispose();
                    throw new Exception($"Download failed: {response.StatusCode}");
                }
            }

            using (response)
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var buffer = new byte[8192];
                var totalRead = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var percentage = (int)((totalRead * 100) / totalBytes);
                            progress?.Report(percentage);
                        }
                    }
                }
            }

            // Launch new installer with same arguments
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            var argsString = string.Join(" ", args.Select(a => $"\"{a}\""));

            var psi = new ProcessStartInfo
            {
                FileName = tempFile,
                Arguments = argsString,
                UseShellExecute = true,
                Verb = "runas" // Request admin
            };

            Process.Start(psi);

            // Exit current installer
            Environment.Exit(0);

            return true;
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] HttpRequestException during installer download: {httpEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Exception details: {httpEx}");
            if (httpEx.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner exception: {httpEx.InnerException.Message}");
            }
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Exception during installer download: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Download application update
    /// </summary>
    public async Task<string?> DownloadApplicationUpdateAsync(string downloadUrl, IProgress<int>? progress = null, long? assetId = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Starting download from: {downloadUrl}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] URL is null/empty: {string.IsNullOrWhiteSpace(downloadUrl)}");

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                System.Diagnostics.Debug.WriteLine("[UpdateService] Download URL is null or empty!");
                DebugLogger.Log("[UpdateService] Download URL is null or empty!");
                return null;
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"EcommerceStarter-Update-{Guid.NewGuid()}.zip");

            System.Diagnostics.Debug.WriteLine($"[UpdateService] Creating Uri from: {downloadUrl}");
            Uri downloadUri;
            try
            {
                downloadUri = new Uri(downloadUrl);
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Uri created successfully: {downloadUri.AbsoluteUri}");
            }
            catch (UriFormatException uriEx)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Invalid URI format: {uriEx.Message}");
                DebugLogger.Log($"[UpdateService] Invalid URI format: {downloadUrl} - {uriEx.Message}");
                return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, downloadUri);

            // Add Authorization header per-request for private repo access
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                System.Diagnostics.Debug.WriteLine("[UpdateService] Adding Authorization header to download request");
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
                DebugLogger.Log("[UpdateService] Using GitHub token for private repo download");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[UpdateService] WARNING: No auth token - download may fail for private repos");
                DebugLogger.Log("[UpdateService] WARNING: No auth token available - download may fail");
            }

            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] HTTP Response: {response.StatusCode}");

                // If direct CDN URL fails with 404 and we have AssetId, try GitHub API endpoint
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound && assetId.HasValue && !string.IsNullOrWhiteSpace(_authToken))
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateService] Direct CDN URL returned 404, falling back to GitHub API endpoint...");
                    response.Dispose();

                    // Use GitHub API endpoint as fallback
                    var apiUrl = $"https://api.github.com/repos/davidtres03/EcommerceStarter/releases/assets/{assetId}";
                    System.Diagnostics.Debug.WriteLine($"[UpdateService] Using API endpoint: {apiUrl}");

                    var apiRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                    apiRequest.Headers.Add("Accept", "application/octet-stream");
                    if (!string.IsNullOrWhiteSpace(_authToken))
                    {
                        apiRequest.Headers.Add("Authorization", $"Bearer {_authToken}");
                    }

                    // Try API endpoint download
                    using (var apiResponse = await _httpClient.SendAsync(apiRequest, HttpCompletionOption.ResponseHeadersRead))
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdateService] API Response: {apiResponse.StatusCode}");
                        apiResponse.EnsureSuccessStatusCode();

                        var apiTotalBytes = apiResponse.Content.Headers.ContentLength ?? 0;
                        System.Diagnostics.Debug.WriteLine($"[UpdateService] Download size: {apiTotalBytes} bytes");
                        var apiBuffer = new byte[8192];
                        var apiTotalRead = 0L;

                        using (var contentStream = await apiResponse.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            int bytesRead;
                            while ((bytesRead = await contentStream.ReadAsync(apiBuffer, 0, apiBuffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(apiBuffer, 0, bytesRead);
                                apiTotalRead += bytesRead;

                                if (apiTotalBytes > 0)
                                {
                                    var progressPercentage = (int)((apiTotalRead * 100) / apiTotalBytes);
                                    progress?.Report(progressPercentage);
                                }
                            }
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[UpdateService] API download complete: {tempFile}");
                    DebugLogger.Log($"[UpdateService] Successfully downloaded update from API endpoint");
                    return tempFile;
                }

                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Download size: {totalBytes} bytes");
                var buffer = new byte[8192];
                var totalRead = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;

                        if (totalBytes > 0)
                        {
                            var percentage = (int)((totalRead * 100) / totalBytes);
                            progress?.Report(percentage);
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"[UpdateService] Download complete: {tempFile}");
            return tempFile;
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] HttpRequestException during download: {httpEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Exception details: {httpEx}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Stack trace: {httpEx.StackTrace}");
            if (httpEx.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner exception: {httpEx.InnerException.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner exception: {httpEx.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner stack: {httpEx.InnerException.StackTrace}");
            }
            DebugLogger.LogException(httpEx, "DownloadApplicationUpdateAsync - HttpRequestException");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Exception during download: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            DebugLogger.LogException(ex, "DownloadApplicationUpdateAsync");
            return null;
        }
    }

    /// <summary>
    /// Get latest release from GitHub
    /// </summary>
    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[UpdateService] GetLatestReleaseAsync called");

            var cacheBuster = DateTime.UtcNow.Ticks;
            var url = $"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest?t={cacheBuster}";
            System.Diagnostics.Debug.WriteLine($"[UpdateService] API URL: {url}");

            System.Diagnostics.Debug.WriteLine("[UpdateService] Creating HttpRequestMessage...");
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));

            // Add Authorization header per-request (thread-safe with shared HttpClient)
            if (!string.IsNullOrWhiteSpace(_authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_authToken}");
                System.Diagnostics.Debug.WriteLine("[UpdateService] Authorization header added to request");
            }

            System.Diagnostics.Debug.WriteLine("[UpdateService] Calling HttpClient.SendAsync...");
            var response = await _httpClient.SendAsync(request);
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] API call failed with status {response.StatusCode}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine("[UpdateService] Reading response content...");
            var json = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Response length: {json.Length} chars");

            System.Diagnostics.Debug.WriteLine("[UpdateService] Deserializing JSON...");
            var result = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            System.Diagnostics.Debug.WriteLine("[UpdateService] JSON deserialized successfully");

            return result;
        }
        catch (InvalidOperationException invalidEx)
        {
            System.Diagnostics.Debug.WriteLine("[UpdateService] InvalidOperationException in GetLatestReleaseAsync");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Message: {invalidEx.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Stack: {invalidEx.StackTrace}");
            if (invalidEx.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner Exception: {invalidEx.InnerException.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[UpdateService] Inner Message: {invalidEx.InnerException.Message}");
            }
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Exception in GetLatestReleaseAsync: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpdateService] Stack: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Get current installer version
    /// </summary>
    private Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        if (version == null)
            return new Version(1, 0, 0);

        // Normalize to 3-part version (Major.Minor.Build) for consistent comparison
        // This handles both 1.0.9.0 (4-part) and 1.0.9 (3-part) the same way
        return new Version(version.Major, version.Minor, version.Build);
    }

    /// <summary>
    /// Parse version from tag name (e.g., "v1.2.3" -> Version(1, 2, 3))
    /// Preserves all version parts (including Revision for 4-part versions like 1.0.9.1)
    /// </summary>
    private Version ParseVersion(string tagName)
    {
        // Remove 'v' prefix if present
        var versionString = tagName.TrimStart('v', 'V');

        if (Version.TryParse(versionString, out var version))
        {
            // Return the full version as-is to preserve 4-part versions like 1.0.9.1
            return version;
        }

        return new Version(1, 0, 0);
    }
}

/// <summary>
/// Information about available updates
/// </summary>
public class UpdateInfo
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public bool HasUpdate { get; set; }
    public string ReleaseNotes { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Information about application updates
/// </summary>
public class ApplicationUpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public long Size { get; set; }
    public long AssetId { get; set; }
}

/// <summary>
/// GitHub Release API response
/// </summary>
internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
}

/// <summary>
/// GitHub Release Asset
/// </summary>
internal class GitHubAsset
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
