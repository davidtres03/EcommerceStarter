using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class UpgradeProgressPage : Page
{
    private readonly ExistingInstallation _existingInstall;
    private readonly UpgradeService _upgradeService;
    private readonly LoggerService _logger;
    private string? _downloadedZipPath;

    public UpgradeProgressPage(ExistingInstallation existingInstall)
    {

        InitializeComponent();
        _existingInstall = existingInstall;

        _logger = new LoggerService();
        
        _upgradeService = new UpgradeService(_logger);

        // Wire up events
        _upgradeService.ProgressUpdate += OnProgressUpdate;
        _upgradeService.StatusUpdate += OnStatusUpdate;


        // Start upgrade process with proper exception handling
        Loaded += async (s, e) =>
        {
            try
            {
                await StartUpgradeAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Unexpected error: {ex.GetType().Name}: {ex.Message}");
            }
        };
    }

    private async Task StartUpgradeAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] === UPGRADE STARTED ===");

            // Step 1: Check for application updates from GitHub
            StatusText.Text = "Checking for updates...";
            System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] Creating UpdateService...");

            try
            {
                var updateService = new UpdateService();
                System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] UpdateService created successfully");

                System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] Checking for application updates...");
                var appUpdate = await updateService.CheckForApplicationUpdatesAsync();

                if (appUpdate == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] No updates found on GitHub");
                    ShowError("No updates found on GitHub. Please create a release first.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Found update: v{appUpdate.Version} at {appUpdate.DownloadUrl}");

                // Log GitHub version to detail window
                UpdateDetailLog($"✓ GitHub Version Available: {appUpdate.Version}");

                // Step 1b: Check if we can use local package (same version as GitHub)
                // This allows running installer from extracted package folder
                var localPackagePath = TryGetLocalPackagePath(appUpdate.Version);
                if (!string.IsNullOrEmpty(localPackagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Found local package at: {localPackagePath}");
                    _downloadedZipPath = localPackagePath;
                    StatusText.Text = "Using local package...";
                    System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] Using local package instead of downloading");
                    ProgressBar.Value = 100;
                    ProgressBar.IsIndeterminate = false;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] No local package found, will download from GitHub");
                }

                // Step 2: Always download fresh from GitHub (no caching)
                // Upgrades should always use the latest package to avoid stale file issues
                if (string.IsNullOrEmpty(_downloadedZipPath))
                {
                    StatusText.Text = $"Downloading v{appUpdate.Version}...";
                    ProgressBar.IsIndeterminate = true;
                    System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Downloading from GitHub: {appUpdate.DownloadUrl}");

                    var downloadProgress = new Progress<int>(percent =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ProgressBar.IsIndeterminate = false;
                            ProgressBar.Value = percent;
                            StatusText.Text = $"Downloading... {percent}%";
                        });
                    });

                    System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] Calling DownloadApplicationUpdateAsync...");
                    _downloadedZipPath = await updateService.DownloadApplicationUpdateAsync(appUpdate.DownloadUrl, downloadProgress, appUpdate.AssetId);

                    if (string.IsNullOrEmpty(_downloadedZipPath))
                    {
                        System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] Download returned null/empty path");
                        ShowError("Failed to download update from GitHub.");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Download complete: {_downloadedZipPath}");
                }

                // Step 3: Perform upgrade
                System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] === BEFORE STEP 3 LOG ===");
                System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] === AFTER STEP 3 LOG ===");

                ProgressBar.Value = 0;

                ProgressBar.IsIndeterminate = false;

                StatusText.Text = "Starting upgrade...";

                System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] Starting upgrade service...");


                try
                {
                }
                catch (Exception logEx1)
                {
                }

                try
                {
                }
                catch (Exception logEx2)
                {
                }

                try
                {
                }
                catch (Exception logEx3)
                {
                }

                UpdateDetailLog($"→ Passing version to upgrade: {appUpdate.Version}");

                UpgradeResult result = null;
                try
                {
                    result = await _upgradeService.UpgradeFromZipAsync(_existingInstall, _downloadedZipPath, appUpdate.Version);
                }
                catch (Exception ex)
                {
                    throw;
                }


                System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Upgrade completed: Success={result.Success}");

                if (result.Success)
                {
                    ShowSuccess(result);
                }
                else
                {
                    ShowError(result.ErrorMessage, result.BackupPath);
                }
            }
            catch (InvalidOperationException invalidEx)
            {
                System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] === InvalidOperationException IN UPGRADE ===");
                System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Message: {invalidEx.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Stack Trace: {invalidEx.StackTrace}");
                if (invalidEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Inner Exception: {invalidEx.InnerException.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Inner Message: {invalidEx.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Inner Stack: {invalidEx.InnerException.StackTrace}");
                }
                ShowError($"Upgrade failed: InvalidOperationException: {invalidEx.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("[UpgradeProgressPage] === UPGRADE EXCEPTION ===");
            System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Exception Type: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Inner Exception: {ex.InnerException.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"[UpgradeProgressPage] Inner Message: {ex.InnerException.Message}");
            }
            ShowError($"Upgrade failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void OnProgressUpdate(object? sender, UpgradeProgress progress)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = progress.Percentage;
            StatusText.Text = progress.Message;
            StepText.Text = $"Step {progress.CurrentStep} of 7";
        });
    }

    private void OnStatusUpdate(object? sender, string status)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {status}";
            DetailText.AppendText($"{logLine}\n");

            // Auto-scroll to bottom
            DetailText.ScrollToEnd();
            
            // Also log to file via LoggerService (will add its own timestamp)
            _logger.Log(status);
        });
    }

    private void UpdateDetailLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}";
            DetailText.AppendText($"{logLine}\n");

            // Auto-scroll to bottom
            DetailText.ScrollToEnd();
            
            // Also log to file via LoggerService (will add its own timestamp)
            _logger.Log(message);
        });
    }

    private void ShowSuccess(UpgradeResult result)
    {
        Dispatcher.Invoke(() =>
        {
            // Hide progress section
            ProgressSection.Visibility = Visibility.Collapsed;
            
            // Show result banner at top with success styling
            ResultPanel.Visibility = Visibility.Visible;
            ResultPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233)); // #E8F5E9 (light green)
            ResultPanel.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // #4CAF50 (green)

            // Set success icon (green background, white checkmark)
            ResultIconBackground.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // #4CAF50
            ResultIconSymbol.Text = "✓";

            ResultTitle.Text = "Upgrade Successful!";
            ResultMessage.Text = result.Message;

            if (!string.IsNullOrEmpty(result.BackupPath))
            {
                ResultMessage.Text += $"\n\nBackup created at:\n{result.BackupPath}";
            }

            // Log final success message to detail window
            UpdateDetailLog("=== UPGRADE COMPLETED SUCCESSFULLY ===");
        });
    }

    private void ShowError(string errorMessage, string? backupPath = null)
    {
        Dispatcher.Invoke(() =>
        {
            // Hide progress section
            ProgressSection.Visibility = Visibility.Collapsed;
            
            // Show result banner at top with error styling
            ResultPanel.Visibility = Visibility.Visible;
            ResultPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 235, 238)); // #FFEBEE (light red)
            ResultPanel.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // #F44336 (red)

            // Set failure icon (red background, white X)
            ResultIconBackground.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // #F44336
            ResultIconSymbol.Text = "✕";

            ResultTitle.Text = "Upgrade Failed";
            ResultMessage.Text = errorMessage;

            if (!string.IsNullOrEmpty(backupPath))
            {
                ResultMessage.Text += $"\n\nBackup available at:\n{backupPath}";
            }

            // Log final error message to detail window
            UpdateDetailLog($"=== UPGRADE FAILED: {errorMessage} ===");
        });
    }

    /// <summary>
    /// Try to locate a local package ZIP in the installer directory
    /// This allows running the installer from the extracted package folder
    /// and using the local migrations instead of downloading from GitHub
    /// Prefers NEWEST available package over exact version match
    /// </summary>
    private string? TryGetLocalPackagePath(string targetVersion)
    {
        try
        {
            // Get the directory where the installer executable is running from
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var installerDir = System.IO.Path.GetDirectoryName(exePath);

            if (string.IsNullOrEmpty(installerDir))
                return null;

            System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] Installer directory: {installerDir}");
            System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] Looking for local package with version: {targetVersion}");

            // First try exact version match
            var packageName = $"EcommerceStarter-Installer-v{targetVersion}.zip";
            var packagePath = System.IO.Path.Combine(installerDir, packageName);

            if (System.IO.File.Exists(packagePath))
            {
                System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] Found exact match local package: {packagePath}");
                return packagePath;
            }

            // Also check parent directory in case installer is in subdirectory
            var parentDir = System.IO.Directory.GetParent(installerDir)?.FullName;
            if (!string.IsNullOrEmpty(parentDir))
            {
                packagePath = System.IO.Path.Combine(parentDir, packageName);
                if (System.IO.File.Exists(packagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] Found exact match in parent directory: {packagePath}");
                    return packagePath;
                }

                // If no exact match, look for ANY newer package in parent directory
                System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] No exact match found, looking for any newer packages...");
                var packageDir = new System.IO.DirectoryInfo(parentDir);
                var allPackages = packageDir.GetFiles("EcommerceStarter-Installer-v*.zip")
                    .OrderByDescending(f => f.Name)
                    .ToList();

                if (allPackages.Count > 0)
                {
                    var newestPackage = allPackages[0].FullName;
                    System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] Found newer package: {newestPackage}");
                    return newestPackage;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] No local package found");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TryGetLocalPackagePath] Exception: {ex.Message}");
            return null;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
