using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcommerceStarter.Installer.Services;
using EcommerceStarter.Installer.Models;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Maintenance mode page for existing installations
/// Provides options to upgrade, reconfigure, repair, or uninstall
/// </summary>
public partial class MaintenanceModePage : Page
{
    private readonly ExistingInstallation _existingInstall;
    private readonly MainWindow _mainWindow;
    private ReleaseInfo? _availableUpdate;

    public MaintenanceModePage(ExistingInstallation existingInstall, MainWindow mainWindow)
    {
        InitializeComponent();
        _existingInstall = existingInstall ?? throw new ArgumentNullException(nameof(existingInstall));
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

        Loaded += MaintenanceModePage_Loaded;
    }

    private async void MaintenanceModePage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadInstallationInfo();
        await CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            var githubService = new GitHubReleaseService();

            // Show debug info while checking
            DebugInfoBorder.Visibility = Visibility.Visible;
            DebugInfoText.Text = "Checking GitHub for latest release...\n";

            var latestRelease = await githubService.GetLatestReleaseAsync();

            if (latestRelease != null)
            {
                // Normalize versions for comparison (remove 'v' prefix if present)
                var currentVer = (_existingInstall.Version ?? "").TrimStart('v');
                var latestVer = (latestRelease.Version ?? "").TrimStart('v');

                // Append debug info
                DebugInfoText.Text += $"GitHub returned: {latestRelease.Version}\n";
                DebugInfoText.Text += $"Normalized: {latestVer}\n";
                DebugInfoText.Text += $"Current: {currentVer}\n";
                DebugInfoText.Text += $"Match: {(latestVer == currentVer ? "YES" : "NO")}\n";

                if (latestVer != currentVer && !string.IsNullOrEmpty(latestVer))
                {
                    _availableUpdate = latestRelease;

                    // Show update available banner
                    UpdateAvailableBanner.Visibility = Visibility.Visible;
                    CurrentVersionDisplay.Text = _existingInstall.Version ?? "Unknown";
                    AvailableVersionDisplay.Text = latestRelease.Version ?? "Unknown";

                    System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Update available: {latestRelease.Version} (current: {_existingInstall.Version})");
                }
                else
                {
                    DebugInfoText.Text += "\n? Versions match - no update needed";
                    // Disable upgrade button when no update available
                    UpgradeButton.IsEnabled = false;
                    UpgradeButton.Opacity = 0.5;
                    System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] No update available. Current: {_existingInstall.Version}, Latest: {latestRelease.Version}");
                }
            }
            else
            {
                DebugInfoText.Text += "ERROR: No release info from GitHub\n";
                System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] No release info available from GitHub");
            }
        }
        catch (Exception ex)
        {
            DebugInfoText.Text += $"ERROR: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Error checking for updates: {ex.Message}");
        }
    }

    private void LoadInstallationInfo()
    {
        try
        {
            // Display installation information
            CompanyNameText.Text = _existingInstall.CompanyName ?? "Unknown Store";
            InstallPathText.Text = _existingInstall.InstallPath ?? "Unknown";
            DatabaseText.Text = $"{_existingInstall.DatabaseServer ?? "Unknown"} / {_existingInstall.DatabaseName ?? "Unknown"}";
            VersionText.Text = _existingInstall.Version ?? "Unknown";
            ProductCountText.Text = _existingInstall.ProductCount.ToString();
            OrderCountText.Text = _existingInstall.OrderCount.ToString();
            UserCountText.Text = _existingInstall.UserCount.ToString();

            // Show health status if available
            if (_existingInstall.IsHealthy)
            {
                HealthStatusBorder.Visibility = Visibility.Visible;
                HealthStatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1F2EB"));
                HealthStatusBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#48C9B0"));
                HealthStatusText.Text = "[?] Installation is healthy";
                HealthStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D5E4F"));
            }
            else if (_existingInstall.Issues != null && _existingInstall.Issues.Any())
            {
                HealthStatusBorder.Visibility = Visibility.Visible;
                HealthStatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3CD"));
                HealthStatusBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                HealthStatusText.Text = "[!] Issues detected:";
                HealthStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#856404"));

                IssuesText.Text = string.Join("\n ", _existingInstall.Issues.Select(i => i));
                IssuesText.Visibility = Visibility.Visible;
                IssuesText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#856404"));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error loading installation information:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Disable button and show loading state to provide visual feedback
            UpgradeButton.IsEnabled = false;
            var originalContent = UpgradeButton.Content;
            UpgradeButton.Content = "? Launching upgrader...";
            UpgradeButton.Cursor = System.Windows.Input.Cursors.Wait;

            // Download the latest release package
            var gitHubService = new Services.GitHubReleaseService();

            System.Diagnostics.Debug.WriteLine("[MaintenanceMode] Fetching latest release...");

            var latestRelease = await gitHubService.GetLatestReleaseAsync();
            if (latestRelease == null)
            {
                MessageBox.Show(
                    "Could not fetch the latest release from GitHub.\n\n" +
                    "Please check your internet connection and try again.",
                    "Download Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Find the ZIP asset - try multiple patterns for compatibility
            var zipAsset = latestRelease.FindAssetByPattern("EcommerceStarter-v*.zip");
            if (zipAsset == null)
            {
                // Try legacy pattern
                zipAsset = latestRelease.FindAssetByPattern("EcommerceStarter-Installer-*.zip");
            }
            if (zipAsset == null)
            {
                // Try any EcommerceStarter ZIP
                zipAsset = latestRelease.FindAssetByPattern("EcommerceStarter-*.zip");
            }

            if (zipAsset == null)
            {
                MessageBox.Show(
                    "Could not find installer package in the release.\n\n" +
                    "The release may be incomplete.",
                    "Package Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Download and extract package to temp folder
            var tempFolder = Path.Combine(Path.GetTempPath(), $"EcommerceStarter-Upgrade-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempFolder);

            var zipPath = Path.Combine(tempFolder, zipAsset.Name);

            System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Downloading to: {zipPath}");

            // Download the ZIP file
            var zipBytes = await gitHubService.DownloadAssetAsync(zipAsset.BrowserDownloadUrl, zipAsset.Id);

            if (zipBytes == null || zipBytes.Length == 0)
            {
                MessageBox.Show(
                    "Failed to download the upgrade package.\n\n" +
                    "Please try again or download manually from GitHub.",
                    "Download Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Write ZIP to disk
            System.IO.File.WriteAllBytes(zipPath, zipBytes);

            // Extract the package
            System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Extracting package...");
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempFolder);

            // Find the upgrader exe in the extracted package
            var upgraderPath = Path.Combine(tempFolder, "Upgrader", "EcommerceStarter.Upgrader.exe");

            if (!File.Exists(upgraderPath))
            {
                // Try alternate location (installer in root - legacy fallback)
                upgraderPath = Path.Combine(tempFolder, "EcommerceStarter.Installer.exe");

                if (!File.Exists(upgraderPath))
                {
                    MessageBox.Show(
                        $"Upgrader not found in package.\n\nExpected at: {Path.Combine(tempFolder, "Upgrader", "EcommerceStarter.Upgrader.exe")}\n\n" +
                        "The download may be corrupted. Please try again.",
                        "Upgrader Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }

            // Build command-line arguments for the upgrader
            var args = $"--sitename \"{_existingInstall.SiteName}\" " +
                      $"--installpath \"{_existingInstall.InstallPath}\" " +
                      $"--dbserver \"{_existingInstall.DatabaseServer}\" " +
                      $"--dbname \"{_existingInstall.DatabaseName}\" " +
                      $"--version \"{_existingInstall.Version}\" " +
                      $"--productcount {_existingInstall.ProductCount} " +
                      $"--ordercount {_existingInstall.OrderCount} " +
                      $"--usercount {_existingInstall.UserCount}";

            System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Launching upgrader: {upgraderPath}");
            System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Arguments: {args}");

            // Launch the upgrader from the downloaded package (not Program Files!)
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = upgraderPath,
                Arguments = args,
                UseShellExecute = true,
                Verb = "runas" // Request admin elevation
            };

            var process = System.Diagnostics.Process.Start(startInfo);

            if (process != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Upgrader launched successfully (PID: {process.Id})");

                // Exit installer immediately and silently to release file locks
                Application.Current.Shutdown();
            }
            else
            {
                MessageBox.Show(
                    "Failed to start the upgrader application.\n\n" +
                    "The upgrade process could not be initiated.",
                    "Launch Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User canceled the UAC prompt
            MessageBox.Show(
                "Administrator privileges are required to upgrade.\n\n" +
                "The upgrade was canceled.",
                "Upgrade Canceled",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MaintenanceMode] Error launching upgrader: {ex.Message}");
            MessageBox.Show(
                $"Error starting upgrade:\n\n{ex.Message}",
                "Upgrade Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Reconfigure_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show confirmation
            var result = MessageBox.Show(
                $"Reconfigure {_existingInstall.CompanyName}?\n\n" +
                "This will allow you to:\n" +
                " Reset the admin password\n" +
                " Update company information\n" +
                " Change Stripe/email settings\n\n" +
                "The application files and database will NOT be modified.",
                "Confirm Reconfigure",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine("[Reconfigure] User confirmed, initializing wizard pages");
                // Initialize wizard pages first (they may not be initialized in maintenance mode)
                _mainWindow.InitializeWizardPages();

                // Now access the ConfigurationPage (index 3)
                var pages = _mainWindow.GetPages();
                System.Diagnostics.Debug.WriteLine($"[Reconfigure] Got {pages.Count} pages");
                System.Diagnostics.Debug.WriteLine($"[Reconfigure] Checking if pages.Count ({pages.Count}) > 3");
                if (pages.Count > 3)
                {
                    var configPage = pages[3] as ConfigurationPage;
                    if (configPage != null)
                    {
                        configPage.LoadExistingConfiguration(_existingInstall);
                        _mainWindow.SetReconfigureMode(true);
                        _mainWindow.NavigateToPageByIndex(3);
                    }
                    else
                    {
                        MessageBox.Show("Error: ConfigurationPage not found", "Reconfigure Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Error: Wizard pages not properly initialized", "Reconfigure Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error starting reconfiguration:\n\n{ex.Message}",
                "Reconfigure Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Repair_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show confirmation
            var result = MessageBox.Show(
                $"Repair {_existingInstall.CompanyName}?\n\n" +
                "This will:\n" +
                " Verify all application files\n" +
                " Replace missing or corrupted files\n" +
                " Repair IIS configuration\n" +
                " Validate database connection\n\n" +
                "Your data will NOT be modified.",
                "Confirm Repair",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine("[Reconfigure] User confirmed, initializing wizard pages");
                // Navigate to repair page
                var repairPage = new RepairPage();
                NavigationService?.Navigate(repairPage);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error starting repair:\n\n{ex.Message}",
                "Repair Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show strong warning
            var result = MessageBox.Show(
                $"UNINSTALL {_existingInstall.CompanyName}?\n\n" +
                $"This will REMOVE:\n" +
                $" Application files from {_existingInstall.InstallPath}\n" +
                $" IIS website and app pool\n" +
                $" Windows registry entries\n\n" +
                $"The DATABASE will be preserved:\n" +
                $" {_existingInstall.DatabaseName} will NOT be deleted\n" +
                $" {_existingInstall.ProductCount} products will remain\n" +
                $" {_existingInstall.OrderCount} orders will remain\n\n" +
                $"Are you SURE you want to uninstall?",
                "?? Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Debug.WriteLine("[Reconfigure] User confirmed, initializing wizard pages");
                // Double confirmation for safety
                var confirm = MessageBox.Show(
                    "Final confirmation:\n\n" +
                    "Click YES to proceed with uninstallation.\n" +
                    "Click NO to cancel and keep the installation.",
                    "Final Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    // Navigate to uninstall page
                    var uninstallPage = new UninstallPage();
                    uninstallPage.SetInstallationToUninstall(_existingInstall);
                    NavigationService?.Navigate(uninstallPage);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error starting uninstall:\n\n{ex.Message}",
                "Uninstall Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Navigate back to instance selection
            _mainWindow.ShowInstanceSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error returning to instance selection:\n\n{ex.Message}",
                "Navigation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
