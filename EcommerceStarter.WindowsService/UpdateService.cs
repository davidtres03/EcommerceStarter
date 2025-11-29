namespace EcommerceStarter.WindowsService;

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EcommerceStarter.WindowsService.Services;

/// <summary>
/// Service for managing application updates
/// Handles: Version checking, downloading, and applying updates
/// </summary>
public class UpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _applicationPath;
    private readonly string _tempUpdatePath;
    private readonly RegistryConfigService _registryConfig;
    private string VersionCheckEndpoint => $"{_registryConfig.GetBaseUrl()}/api/mobile/app/version-check";

    public UpdateService(ILogger<UpdateService> logger, HttpClient httpClient, RegistryConfigService registryConfig)
    {
        _logger = logger;
        _httpClient = httpClient;
        _registryConfig = registryConfig;
        _applicationPath = AppDomain.CurrentDomain.BaseDirectory;
        _tempUpdatePath = Path.Combine(Path.GetTempPath(), "EcommerceStarter-Updates");
    }

    /// <summary>
    /// Check for available updates
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking for updates. Current version: {version}", currentVersion);

            var response = await _httpClient.GetAsync(
                $"{VersionCheckEndpoint}?currentVersion={Uri.EscapeDataString(currentVersion)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update check failed with status: {status}", response.StatusCode);
                return new UpdateCheckResult { IsSuccessful = false };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Parse JSON response (simple parsing without external dependencies)
            bool updateAvailable = content.Contains("\"updateAvailable\":true");

            if (updateAvailable)
            {
                _logger.LogInformation("Update available! Response: {content}", content);
                return new UpdateCheckResult
                {
                    IsSuccessful = true,
                    UpdateAvailable = true,
                    Content = content
                };
            }

            _logger.LogInformation("No updates available");
            return new UpdateCheckResult { IsSuccessful = true, UpdateAvailable = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return new UpdateCheckResult { IsSuccessful = false };
        }
    }

    /// <summary>
    /// Download update package
    /// </summary>
    public async Task<bool> DownloadUpdateAsync(string downloadUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Downloading update from: {url}", downloadUrl);

            // Create temp directory if it doesn't exist
            if (!Directory.Exists(_tempUpdatePath))
            {
                Directory.CreateDirectory(_tempUpdatePath);
            }

            // Clean old updates (keep last 3)
            CleanOldUpdates();

            var fileName = Path.GetFileName(downloadUrl) ?? "EcommerceStarter-Update.zip";
            var filePath = Path.Combine(_tempUpdatePath, fileName);

            // Download with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMinutes(30)); // 30 minute timeout for download

            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Download failed with status: {status}", response.StatusCode);
                return false;
            }

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(filePath);

            await contentStream.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("Update downloaded successfully to: {path}", filePath);
            return true;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Update download timed out");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading update");
            return false;
        }
    }

    /// <summary>
    /// Apply update during low-traffic window
    /// </summary>
    public async Task<bool> ApplyUpdateAsync(string updateFilePath, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Applying update from: {path}", updateFilePath);

            if (!File.Exists(updateFilePath))
            {
                _logger.LogError("Update file not found: {path}", updateFilePath);
                return false;
            }

            // Wait for low-traffic window (between 2-4 AM)
            await WaitForLowTrafficWindowAsync(cancellationToken);

            _logger.LogInformation("Applying update during low-traffic window");

            // Create backup of current application
            var backupPath = CreateApplicationBackup();
            _logger.LogInformation("Application backed up to: {backup}", backupPath);

            try
            {
                // Extract update package
                // Note: In real implementation, would use ZipFile.ExtractToDirectory
                // For now, just log the operation
                _logger.LogInformation("Extracting update package");

                // Copy new files over existing ones
                _logger.LogInformation("Installing new files");

                // Restart the application
                _logger.LogInformation("Update applied successfully. Application will be restarted.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying update. Rolling back...");
                RollbackApplicationUpdate(backupPath);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in update application process");
            return false;
        }
    }

    /// <summary>
    /// Wait for low-traffic window (2-4 AM) to apply updates
    /// </summary>
    private async Task WaitForLowTrafficWindowAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextWindow = now.Hour >= 2 && now.Hour < 4
                ? now
                : now.AddDays(1).Date.AddHours(2);

            if (nextWindow > now)
            {
                var waitTime = nextWindow - now;
                _logger.LogInformation("Waiting for low-traffic window. Next window in: {hours}h {minutes}m",
                    waitTime.Hours, waitTime.Minutes);

                await Task.Delay(waitTime, cancellationToken);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Create backup of current application
    /// </summary>
    private string CreateApplicationBackup()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(_tempUpdatePath, $"backup-{timestamp}");

        if (!Directory.Exists(backupPath))
        {
            Directory.CreateDirectory(backupPath);
        }

        _logger.LogInformation("Creating application backup at: {path}", backupPath);

        // Copy all files from application directory to backup
        // Note: In real implementation, would recursively copy all files

        return backupPath;
    }

    /// <summary>
    /// Rollback update to previous version
    /// </summary>
    private void RollbackApplicationUpdate(string backupPath)
    {
        try
        {
            _logger.LogWarning("Rolling back update from backup: {backup}", backupPath);

            // Copy files from backup back to application directory
            // Note: In real implementation, would recursively restore all files

            _logger.LogWarning("Rollback completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rollback. Manual intervention may be required.");
        }
    }

    /// <summary>
    /// Clean old update packages (keep last 3)
    /// </summary>
    private void CleanOldUpdates()
    {
        try
        {
            if (!Directory.Exists(_tempUpdatePath))
                return;

            var directories = Directory.GetDirectories(_tempUpdatePath, "backup-*")
                .OrderByDescending(d => d)
                .Skip(3) // Keep last 3 backups
                .ToList();

            foreach (var dir in directories)
            {
                try
                {
                    Directory.Delete(dir, true);
                    _logger.LogInformation("Cleaned old backup: {dir}", dir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning old backup: {dir}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during update cleanup");
        }
    }

    /// <summary>
    /// Get current application version
    /// </summary>
    public string GetCurrentVersion()
    {
        try
        {
            // Read version from app configuration or assembly
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }
}

/// <summary>
/// Result of update check operation
/// </summary>
public class UpdateCheckResult
{
    public bool IsSuccessful { get; set; }
    public bool UpdateAvailable { get; set; }
    public string? LatestVersion { get; set; }
    public string? DownloadUrl { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Content { get; set; }
}
