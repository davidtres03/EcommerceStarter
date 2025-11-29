using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;
using EcommerceStarter.Installer.Models;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// UpdateCheckPage.xaml - Version checking and update download UI
/// Checks GitHub for latest release and downloads if available
/// </summary>
public partial class UpdateCheckPage : Page
{
    private readonly GitHubReleaseService _githubService;
    private readonly CacheService _cacheService;
    private ReleaseInfo? _latestRelease;
    private bool _updateAvailable = false;
    private bool _isCheckComplete = false;

    /// <summary>
    /// Indicates if the update check has completed
    /// MainWindow should disable Next button until this is true
    /// </summary>
    public bool IsCheckComplete
    {
        get => _isCheckComplete;
        set => _isCheckComplete = value;
    }

    public UpdateCheckPage()
    {
        InitializeComponent();
        _githubService = new GitHubReleaseService();
        _cacheService = new CacheService();
        IsVisibleChanged += UpdateCheckPage_VisibleChanged;
    }

    private void UpdateCheckPage_VisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            UpdateCheckPage_Loaded(sender, new RoutedEventArgs());
        }
    }

    private async void UpdateCheckPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Mark check as starting
            IsCheckComplete = false;

            // RESET ALL UI FIELDS TO CLEAR STALE DATA
            _latestRelease = null;
            _updateAvailable = false;
            LatestVersionText.Text = "Checking...";
            ChangelogText.Text = "";
            DownloadProgressBar.Value = 0;
            PercentageText.Text = "0%";
            InfoMessage.Text = "";

            // Get current version from assembly
            var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            // Use full version including revision (support both 3-part and 4-part formats)
            var currentVersion = assemblyVersion != null
                ? (assemblyVersion.Revision > 0
                    ? $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}.{assemblyVersion.Revision}"
                    : $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}")
                : "0.9.0";
            CurrentVersionText.Text = $"v{currentVersion}";
            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Current version: {currentVersion} (from assembly: {assemblyVersion?.ToString() ?? "null"})");

            // Check for latest release
            StatusMessage.Text = "Connecting to GitHub to check for the latest version...";
            System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Starting GitHub release check...");

            try
            {
                _latestRelease = await _githubService.GetLatestReleaseAsync();
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] GitHub API call completed. Release: {_latestRelease?.Version ?? "null"}");
            }
            catch (Exception ex)
            {
                // GitHub check failed - proceed anyway
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] ERROR during GitHub check: {ex.Message}");
                StatusMessage.Text = "Unable to check for updates at this time. Proceeding with current version.";
                LatestVersionText.Text = "N/A";
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = 100;
                PercentageText.Text = "100%";
                InfoMessage.Text = "GitHub check failed. You can continue with the current installation.";
                ChangelogText.Text = $"Connection error: {ex.Message}";
                return;
            }

            if (_latestRelease == null)
            {
                System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Latest release is null");
                StatusMessage.Text = "No releases found on GitHub. Proceeding with current version.";
                LatestVersionText.Text = "N/A";
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = 100;
                PercentageText.Text = "100%";
                InfoMessage.Text = "Your installation is up to date. Click 'Next' to continue.";
                ChangelogText.Text = "No releases available. Your system is running the current stable version.";
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Latest release version: {_latestRelease.Version}");
            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Number of assets: {_latestRelease.Assets?.Count ?? 0}");
            if (_latestRelease.Assets != null)
            {
                foreach (var asset in _latestRelease.Assets)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage]   - Asset: {asset.Name} ({asset.Size} bytes)");
                }
            }

            LatestVersionText.Text = _latestRelease.Version;

            // Sanitize release description - remove broken emoji characters
            var cleanDescription = SanitizeText(_latestRelease.Description ?? "No release notes available.");
            _latestRelease.Description = cleanDescription;

            // Check if update is available
            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Comparing versions: latest={_latestRelease.Version} vs current={currentVersion}");
            if (IsNewerVersion(_latestRelease.Version, currentVersion))
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Update IS available!");
                _updateAvailable = true;
                StatusMessage.Text = "New version available! Downloading...";
                InfoMessage.Text = "A newer version has been found and will be downloaded.";

                // Find the application ZIP asset (support both naming patterns)
                System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Looking for ZIP asset with pattern: EcommerceStarter-*.zip");
                var appAsset = _latestRelease.FindAssetByPattern("EcommerceStarter-*.zip");
                if (appAsset == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] First pattern failed, trying: EcommerceStarter-Installer-*.zip");
                    // Try alternate pattern
                    appAsset = _latestRelease.FindAssetByPattern("EcommerceStarter-Installer-*.zip");
                }

                if (appAsset != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Found asset: {appAsset.Name} ({appAsset.Size} bytes)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] ERROR: No ZIP asset found!");
                }

                if (appAsset == null)
                {
                    StatusMessage.Text = "Error: No application package found in release.";
                    DownloadProgressBar.IsIndeterminate = false;
                    InfoMessage.Text = $"The release exists but has no installer package. Available assets: {string.Join(", ", _latestRelease.Assets.Select(a => a.Name))}";
                    return;
                }

                // Display release notes
                ChangelogText.Text = _latestRelease.Description ?? "No release notes available.";
                DownloadSizeText.Text = FormatBytes(appAsset.Size);

                // Start download with progress tracking
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Starting download from: {appAsset.BrowserDownloadUrl}");
                var progress = new Progress<DownloadProgress>(report =>
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Download progress: {report.PercentComplete:F0}% ({FormatBytes(report.BytesReceived)}/{FormatBytes(report.TotalBytes)})");
                    Dispatcher.Invoke(() =>
                    {
                        DownloadProgressBar.IsIndeterminate = false;
                        DownloadProgressBar.Value = report.PercentComplete;
                        ProgressText.Text = $"{FormatBytes(report.BytesReceived)} / {FormatBytes(report.TotalBytes)}";
                        PercentageText.Text = $"{report.PercentComplete:F0}%";
                        SpeedText.Text = $"{report.SpeedMBps:F2} MB/s";
                        ETAText.Text = report.ETA.TotalSeconds > 0 ? report.ETA.ToString(@"mm\:ss") : "Calculating...";
                    });
                });

                // Download the asset
                try
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Calling DownloadAssetAsync...");
                    var downloadedData = await _githubService.DownloadAssetAsync(appAsset.BrowserDownloadUrl, appAsset.Id, progress);
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Download completed! Received {downloadedData.Length} bytes");

                    // Cache the downloaded data
                    await _cacheService.CacheDownloadAsync(_latestRelease.Version, appAsset.Name, downloadedData);
                    System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Download cached successfully");

                    StatusMessage.Text = "Update downloaded successfully!";
                    InfoMessage.Text = "Installation package is ready. Click 'Next' to apply the update.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] ERROR during download: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage] Exception details: {ex}");
                    throw;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] No update available - versions are equal or current is newer");
                _updateAvailable = false;
                StatusMessage.Text = "Your installation is up to date.";
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = 100;
                PercentageText.Text = "100%";
                InfoMessage.Text = "You are running the latest version. Click 'Next' to continue.";
                ChangelogText.Text = "No updates available. Your system is running the current stable version.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage.Text = "Error checking for updates.";
            InfoMessage.Text = $"Details: {ex.Message}";
            DownloadProgressBar.IsIndeterminate = false;
        }
        finally
        {
            // Mark check as complete and enable action buttons
            IsCheckComplete = true;
            Dispatcher.Invoke(() =>
            {
                SkipButton.IsEnabled = true;
                if (_updateAvailable)
                {
                    UpgradeButton.IsEnabled = true;
                }
            });
            System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Update check complete - IsCheckComplete = true");
        }
    }

    /// <summary>
    /// Compare two version strings to determine if newer is greater than current
    /// </summary>
    private static bool IsNewerVersion(string newer, string current)
    {
        try
        {
            // Remove 'v' prefix if present
            var newerClean = newer.TrimStart('v');
            var currentClean = current.TrimStart('v');

            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage.IsNewerVersion] Comparing: newer='{newerClean}' vs current='{currentClean}'");

            if (Version.TryParse(newerClean, out var newerVersion) &&
                Version.TryParse(currentClean, out var currentVersion))
            {
                var isNewer = newerVersion > currentVersion;
                System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage.IsNewerVersion] Parsed versions: newer={newerVersion} vs current={currentVersion} -> isNewer={isNewer}");
                return isNewer;
            }

            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage.IsNewerVersion] Failed to parse versions!");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateCheckPage.IsNewerVersion] Exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Format bytes to human-readable format (B, KB, MB, GB)
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }

    /// <summary>
    /// Sanitize text by removing broken/invalid UTF-8 characters
    /// Keeps only printable ASCII and common extended characters
    /// </summary>
    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Replace broken emoji markers with spaces, keep everything else
        var result = System.Text.RegularExpressions.Regex.Replace(text, @"[\uFFFD?]", " ");

        // Clean up multiple spaces
        result = System.Text.RegularExpressions.Regex.Replace(result, @" +", " ");

        return result.Trim();
    }

    /// <summary>
    /// Public property to check if update was available (used by MainWindow for navigation)
    /// </summary>
    public bool UpdateAvailable => _updateAvailable;

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Skip button clicked");
        // Navigate to next page without upgrading
        // The MainWindow will handle the navigation when we return from this page
        if (this.Parent is System.Windows.Controls.Frame frame)
        {
            frame.GoBack();
        }
    }

    private void UpgradeButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("[UpdateCheckPage] Upgrade button clicked");
        // Navigate to upgrade process
        if (this.Parent is System.Windows.Controls.Frame frame)
        {
            // The MainWindow will detect that upgrade is ready and show UpgradeProgressPage
            frame.Navigate(new UpgradeProgressPage(null)); // Will be set by MainWindow
        }
    }

    /// <summary>
    /// Public property to get the latest release info (for UI display purposes)
    /// </summary>
    public ReleaseInfo? LatestRelease => _latestRelease;
}