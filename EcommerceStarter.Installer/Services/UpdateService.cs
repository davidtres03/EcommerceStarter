using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
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
    
    public UpdateService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EcommerceStarter-Installer");
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
                return null;
            
            // Find the application zip file
            var appAsset = latestRelease.Assets.FirstOrDefault(a => 
                a.Name.StartsWith("EcommerceStarter-") && a.Name.EndsWith(".zip"));
            
            if (appAsset == null)
                return null;
            
            return new ApplicationUpdateInfo
            {
                Version = ParseVersion(latestRelease.TagName).ToString(),
                DownloadUrl = appAsset.BrowserDownloadUrl,
                ReleaseNotes = latestRelease.Body,
                PublishedAt = latestRelease.PublishedAt,
                Size = appAsset.Size
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Download and apply installer update
    /// </summary>
    public async Task<bool> DownloadAndApplyInstallerUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
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
            
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
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
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Download application update
    /// </summary>
    public async Task<string?> DownloadApplicationUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
    {
        try
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"EcommerceStarter-Update-{Guid.NewGuid()}.zip");
            
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
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
            
            return tempFile;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Get latest release from GitHub
    /// </summary>
    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        var url = $"{GITHUB_API_BASE}/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest";
        
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
            return null;
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
    }
    
    /// <summary>
    /// Get current installer version
    /// </summary>
    private Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version ?? new Version(1, 0, 0);
    }
    
    /// <summary>
    /// Parse version from tag name (e.g., "v1.2.3" -> Version(1, 2, 3))
    /// </summary>
    private Version ParseVersion(string tagName)
    {
        // Remove 'v' prefix if present
        var versionString = tagName.TrimStart('v', 'V');
        
        if (Version.TryParse(versionString, out var version))
            return version;
        
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
}

/// <summary>
/// GitHub Release API response
/// </summary>
internal class GitHubRelease
{
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public GitHubAsset[] Assets { get; set; } = Array.Empty<GitHubAsset>();
}

/// <summary>
/// GitHub Release Asset
/// </summary>
internal class GitHubAsset
{
    public string Name { get; set; } = string.Empty;
    public string BrowserDownloadUrl { get; set; } = string.Empty;
    public long Size { get; set; }
}
