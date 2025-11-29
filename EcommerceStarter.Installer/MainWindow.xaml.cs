using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using EcommerceStarter.Installer.Views;
using EcommerceStarter.Installer.Models;
using EcommerceStarter.Installer.Services;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace EcommerceStarter.Installer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly List<Page> _pages = new();
    private int _currentPageIndex = 0;
    private InstallationConfig? _savedConfig;
    private bool _isReconfigureMode = false;
    private bool _isNewInstallFlow = false; // Controls wizard composition (skip upgrade UI)
    public bool ForceNewInstall { get; set; } = false; // Allows App to bypass instance selection

    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigated += ContentFrame_Navigated;
        Loaded += MainWindow_Loaded;
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        try
        {
            if (e.Content is Page page)
            {
                // If the page root is not a ScrollViewer, wrap it to ensure scrolling on smaller screens
                if (page.Content is not ScrollViewer && page.Content is UIElement element)
                {
                    var scroller = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        CanContentScroll = true,
                        Content = element
                    };
                    page.Content = scroller;
                }
            }
        }
        catch { }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var logFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EcommerceStarter_Installer_Debug.log");

        void Log(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                System.IO.File.AppendAllText(logFile, $"[{timestamp}] {message}\n");
                System.Diagnostics.Debug.WriteLine(message);
            }
            catch { }
        }

        try
        {
            Log("[MainWindow] === MAINWINDOW LOADED ===");
            Log($"[MainWindow] Log file: {logFile}");

            // If forced new install (e.g., --new flag), skip instance selection entirely
            if (ForceNewInstall)
            {
                Log("[MainWindow] ForceNewInstall=true → initializing fresh install wizard");
                _isNewInstallFlow = true;
                InitializeWizard();
                return;
            }

            // Check for ALL existing installations
            Log("[MainWindow] Creating UpgradeDetectionService...");
            var upgradeDetection = new UpgradeDetectionService();
            Log("[MainWindow] UpgradeDetectionService created successfully");

            Log("[MainWindow] Calling DetectAllInstallationsAsync...");
            List<ExistingInstallation> existingInstallations;
            try
            {
                existingInstallations = await upgradeDetection.DetectAllInstallationsAsync();
                Log($"[MainWindow] Detection completed - found {existingInstallations.Count} installations");
            }
            catch (Exception detectionEx)
            {
                Log($"[MainWindow] DETECTION EXCEPTION: {detectionEx.GetType().Name}");
                Log($"[MainWindow] Message: {detectionEx.Message}");
                Log($"[MainWindow] Stack: {detectionEx.StackTrace}");

                MessageBox.Show(
                    $"Error detecting existing installations:\n\n{detectionEx.Message}\n\nLog file:\n{logFile}\n\nProceeding with fresh installation wizard.",
                    "Detection Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                existingInstallations = new List<ExistingInstallation>();
            }

            // Default: show instance selection as the entry screen
            Log($"[MainWindow] Showing instance selection (found {existingInstallations.Count})");
            var instanceSelectionPage = new InstanceSelectionPage(existingInstallations, this);
            Log("[MainWindow] InstanceSelectionPage created");

            ContentFrame.Navigate(instanceSelectionPage);
            Log("[MainWindow] Navigation completed");

            // Hide standard wizard navigation for instance selection
            HideStandardNavigation();
            Log("[MainWindow] Standard navigation hidden");
            return; // Instance selection handles further navigation
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] FATAL ERROR in MainWindow_Loaded: {ex}");

            MessageBox.Show(
                $"Critical error during installer startup:\n\n{ex.GetType().Name}: {ex.Message}\n\nStack trace:\n{ex.StackTrace}\n\nThe installer will attempt to continue with a fresh installation.",
                "Critical Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Try to recover by initializing a fresh wizard
            try
            {
                InitializeWizard();
            }
            catch (Exception initEx)
            {
                MessageBox.Show(
                    $"Failed to recover from startup error:\n\n{initEx.Message}\n\nThe installer must close.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }
    }

    private void InitializeWizard()
    {
        // Reset and create wizard pages in order
        _pages.Clear();
        if (_isNewInstallFlow)
        {
            // New installation: do NOT show Update/Upgrade page
            // Pass flag to WelcomePage so it doesn't auto-switch to reconfiguration
            _pages.Add(new WelcomePage(suppressReconfigDetection: true));
            _pages.Add(new PrerequisitesPage());
            _pages.Add(new ConfigurationPage());
            _pages.Add(new InstallationPage());
            _pages.Add(new CompletionPage());
        }
        else
        {
            // Default flow (e.g., invoked by other modes): include UpdateCheckPage
            _pages.Add(new UpdateCheckPage());
            _pages.Add(new WelcomePage());
            _pages.Add(new PrerequisitesPage());
            _pages.Add(new ConfigurationPage());
            _pages.Add(new InstallationPage());
            _pages.Add(new CompletionPage());
        }

        // Ensure standard navigation is visible when starting the wizard
        ShowStandardNavigation();

        // Navigate to first page
        NavigateToPage(0);
    }

    /// <summary>
    /// Public entry point to start a brand-new installation wizard from the instance list.
    /// Ensures navigation chrome is visible and pages are initialized from a clean slate.
    /// </summary>
    public void StartNewInstallationWizard()
    {
        _isNewInstallFlow = true;
        ShowStandardNavigation();
        InitializeWizard();
    }

    /// <summary>
    /// Initialize wizard pages without navigation (for maintenance mode)
    /// </summary>
    public void InitializeWizardPages()
    {
        if (_pages.Count > 0)
            return; // Already initialized

        // Create wizard pages in order
        _pages.Add(new UpdateCheckPage());
        _pages.Add(new WelcomePage());
        _pages.Add(new PrerequisitesPage());
        _pages.Add(new ConfigurationPage());
        _pages.Add(new InstallationPage());
        _pages.Add(new CompletionPage());
    }

    private bool IsReconfigurationMode()
    {
        // Check if we're in reconfiguration mode
        var welcomePage = _pages.OfType<WelcomePage>().FirstOrDefault();
        return welcomePage?.IsReconfiguration ?? false;
    }

    private void NavigateToPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= _pages.Count)
            return;

        _currentPageIndex = pageIndex;

        // Animate page transition
        var fadeIn = (Storyboard)FindResource("PageFadeIn");
        fadeIn.Begin(ContentFrame);

        // Navigate to page
        ContentFrame.Navigate(_pages[pageIndex]);

        // Update navigation buttons
        UpdateNavigationButtons();

        // Update step indicator
        StepIndicator.Text = $"Step {pageIndex + 1} of {_pages.Count}";
    }

    private void UpdateNavigationButtons()
    {
        // Back button
        BackButton.IsEnabled = _currentPageIndex > 0;

        // Check if we're on Installation page showing summary
        bool isInstallationSummary = _currentPageIndex == _pages.Count - 2 &&
                                     _pages[_currentPageIndex] is InstallationPage installPage &&
                                     !installPage.IsInstallationComplete();

        // Next/Install button
        if (_currentPageIndex == _pages.Count - 1)
        {
            // Last page - Finish button
            NextButton.Content = "Finish";
            NextButton.IsEnabled = true;
        }
        else if (isInstallationSummary)
        {
            // Installation page (summary view) - Install button
            NextButton.Content = "Install";
            NextButton.IsEnabled = true;
        }
        else
        {
            // Regular Next button
            NextButton.Content = "Next →";
            NextButton.IsEnabled = true;
        }

        // Hide Cancel button on last page
        CancelButton.Visibility = _currentPageIndex == _pages.Count - 1
            ? Visibility.Collapsed
            : Visibility.Visible;

        // Update step indicator (adjusted for skipped Prerequisites in reconfiguration mode)
        int displayStep = _currentPageIndex + 1;
        int totalSteps = _pages.Count;

        if (IsReconfigurationMode())
        {
            // In reconfiguration mode, we skip Prerequisites page
            totalSteps = _pages.Count - 1; // One less step
            if (_currentPageIndex >= 2)
            {
                // After Prerequisites page, adjust the step number down by 1
                displayStep = _currentPageIndex; // 2 becomes 2, 3 becomes 3, etc.
            }
        }

        StepIndicator.Text = $"Step {displayStep} of {totalSteps}";
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPageIndex > 0)
        {
            // Skip Prerequisites page when going back in reconfiguration mode
            if (_currentPageIndex == 2 && IsReconfigurationMode())
            {
                NavigateToPage(0); // Jump to WelcomePage
            }
            else
            {
                NavigateToPage(_currentPageIndex - 1);
            }
        }
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        var logMsg = $"[NextButton_Click] _currentPageIndex = {_currentPageIndex}, _pages.Count = {_pages.Count}";
        LogToFile(logMsg);

        var pageMsg = $"[NextButton_Click] Current page type = {_pages[_currentPageIndex]?.GetType().Name}";
        LogToFile(pageMsg);

        try
        {
            // Check if on UpdateCheckPage and if check is still in progress
            if (_pages[_currentPageIndex] is UpdateCheckPage updateCheckPage)
            {
                if (!updateCheckPage.IsCheckComplete)
                {
                    LogToFile("[NextButton_Click] Update check still in progress, please wait...");
                    System.Windows.MessageBox.Show(
                        "Please wait for the update check to complete.",
                        "Update Check In Progress",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                    return;
                }
            }

            if (_currentPageIndex == _pages.Count - 1)
            {
                LogToFile("[NextButton_Click] At last page, closing");
                // Finish - close the installer
                Application.Current.Shutdown();
            }
            else if (_currentPageIndex == _pages.Count - 2 && _pages[_currentPageIndex] is InstallationPage installPage)
            {
                LogToFile("[NextButton_Click] On Installation page");
                // Installation page - trigger installation
                if (!installPage.IsInstallationComplete())
                {
                    // Disable navigation during installation
                    BackButton.IsEnabled = false;
                    NextButton.IsEnabled = false;
                    CancelButton.IsEnabled = false;

                    // Start installation
                    await installPage.StartInstallationAsync();

                    // Re-enable navigation
                    BackButton.IsEnabled = true;
                    NextButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;

                    UpdateNavigationButtons();
                }
                else
                {
                    // Installation complete, pass config to completion page
                    if (_pages[_currentPageIndex + 1] is CompletionPage completionPage && _savedConfig != null)
                    {
                        completionPage.SetConfiguration(_savedConfig);
                    }

                    NavigateToPage(_currentPageIndex + 1);
                }
            }
            else if (_currentPageIndex < _pages.Count - 1)
            {
                LogToFile("[NextButton_Click] Calling ValidateCurrentPage()");
                // Validate current page before moving forward
                if (ValidateCurrentPage())
                {
                    LogToFile("[NextButton_Click] ValidateCurrentPage() returned TRUE - proceeding");

                    // Capture and save configuration when leaving Configuration page
                    if (_pages[_currentPageIndex] is ConfigurationPage configPage)
                    {
                        _savedConfig = configPage.GetConfiguration();

                        // If in reconfigure mode, save and return to instance selection
                        if (_isReconfigureMode)
                        {
                            try
                            {
                                // Save the reconfiguration and get detailed results
                                var rc = await SaveReconfigurationAsync(_savedConfig);

                                var sb = new StringBuilder();
                                sb.AppendLine("Reconfigure results:");
                                sb.AppendLine($"- Connection string: {(rc.ConnectionUpdated ? "OK" : rc.ConnectionAttempted ? "Failed" : "Skipped")}");
                                if (!string.IsNullOrWhiteSpace(rc.ConnectionError)) sb.AppendLine($"  Error: {rc.ConnectionError}");
                                sb.AppendLine($"- Admin user: {(rc.AdminUpdated ? "OK" : rc.AdminAttempted ? "Failed" : "Skipped")}");
                                if (!string.IsNullOrWhiteSpace(rc.AdminError)) sb.AppendLine($"  Error: {rc.AdminError}");
                                sb.AppendLine($"- Registry: {(rc.RegistryUpdated ? "OK" : "Skipped/Failed")}");
                                if (!string.IsNullOrWhiteSpace(rc.RegistryError)) sb.AppendLine($"  Error: {rc.RegistryError}");
                                sb.AppendLine($"- web.config: {(rc.WebConfigUpdated ? "OK" : rc.WebConfigAttempted ? "Failed" : "Skipped")}");
                                if (!string.IsNullOrWhiteSpace(rc.WebConfigError)) sb.AppendLine($"  Error: {rc.WebConfigError}");

                                var hasErrors = !string.IsNullOrEmpty(rc.ConnectionError) || !string.IsNullOrEmpty(rc.AdminError) || !string.IsNullOrEmpty(rc.RegistryError) || !string.IsNullOrEmpty(rc.WebConfigError);
                                MessageBox.Show(
                                    sb.ToString(),
                                    hasErrors ? "Reconfigure Completed With Issues" : "Reconfigure Complete",
                                    MessageBoxButton.OK,
                                    hasErrors ? MessageBoxImage.Warning : MessageBoxImage.Information);

                                // Return to instance selection
                                ShowInstanceSelection();
                                return;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(
                                    $"Error saving reconfiguration:\n\n{ex.Message}",
                                    "Reconfigure Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                                return;
                            }
                        }
                    }

                    // Skip Prerequisites page in reconfiguration mode
                    if (_currentPageIndex == 0 && IsReconfigurationMode())
                    {
                        NavigateToPage(2); // Jump to ConfigurationPage
                    }
                    else
                    {
                        NavigateToPage(_currentPageIndex + 1);
                    }
                }
                else
                {
                    LogToFile("[NextButton_Click] ValidateCurrentPage() returned FALSE - blocking navigation");
                }
            }
            else
            {
                LogToFile($"[NextButton_Click] Unexpected state: _currentPageIndex={_currentPageIndex}, _pages.Count={_pages.Count}");
            }
        }
        catch (Exception ex)
        {
            LogToFile($"[NextButton_Click] ERROR: {ex}");
            MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "NextButton Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogToFile(string message)
    {
        try
        {
            var logFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EcommerceStarter_Installer.log");
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            var fullMessage = $"{timestamp} {message}";
            System.IO.File.AppendAllText(logFile, fullMessage + Environment.NewLine);
        }
        catch { }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to cancel the installation?",
            "Cancel Installation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Application.Current.Shutdown();
        }
    }

    private bool ValidateCurrentPage()
    {
        // Page-specific validation
        var currentPage = _pages[_currentPageIndex];

        // Configuration page validation
        if (currentPage is ConfigurationPage configPage)
        {
            var isValid = configPage.IsFormValid();
            LogToFile($"[ValidateCurrentPage] ConfigurationPage.IsFormValid() = {isValid}");
            return isValid;
        }

        // Other pages - allow navigation
        LogToFile($"[ValidateCurrentPage] Non-config page ({currentPage?.GetType().Name}), allowing navigation");
        return true;
    }

    public List<Page> GetPages()
    {
        return _pages;
    }

    public InstallationConfig? GetSavedConfiguration()
    {
        return _savedConfig;
    }

    public void NavigateToPageByIndex(int index)
    {
        NavigateToPage(index);
    }

    /// <summary>
    /// Set reconfigure mode (called from MaintenanceModePage)
    /// </summary>
    public void SetReconfigureMode(bool isReconfigureMode)
    {
        _isReconfigureMode = isReconfigureMode;

        if (isReconfigureMode)
        {
            // Show simplified navigation for reconfigure
            BackButton.Visibility = Visibility.Visible;
            NextButton.Visibility = Visibility.Visible;
            CancelButton.Visibility = Visibility.Visible;
            StepIndicator.Visibility = Visibility.Collapsed; // Hide step indicator

            // Set current page index to ConfigurationPage (index 3)
            // This allows Next button to work properly
            _currentPageIndex = 3; // ConfigurationPage index

            System.Diagnostics.Debug.WriteLine($"[MainWindow.SetReconfigureMode] Set _currentPageIndex to {_currentPageIndex}");
        }
    }

    /// <summary>
    /// Launch a specific demo scenario
    /// </summary>
    public void LaunchDemoScenario(DemoScenario scenario)
    {
        Loaded -= MainWindow_Loaded; // Remove normal loaded event
        Loaded += (s, e) => LaunchDemoScenarioInternal(scenario);
    }

    private async void LaunchDemoScenarioInternal(DemoScenario scenario)
    {
        try
        {
            // Create mock existing installation for demos that need it
            var mockInstall = new Services.ExistingInstallation
            {
                CompanyName = "EcommerceStarter Supply Co.",
                InstallPath = @"C:\inetpub\wwwroot\EcommerceStarter",
                DatabaseServer = "localhost\\SQLEXPRESS",
                DatabaseName = "EcommerceStarter",
                Version = "1.0.0",
                ProductCount = 245,
                OrderCount = 89,
                UserCount = 3,
                IsHealthy = true,
                Issues = new System.Collections.Generic.List<string>()
            };

            switch (scenario)
            {
                case DemoScenario.FreshInstall:
                    // Show fresh install wizard
                    _isNewInstallFlow = true;
                    InitializeWizard();
                    break;

                case DemoScenario.Upgrade:
                    // Show upgrade welcome page with mock data
                    var upgradePage = new UpgradeWelcomePage(mockInstall);
                    ContentFrame.Navigate(upgradePage);
                    HideStandardNavigation();
                    break;

                case DemoScenario.Reconfigure:
                    // Show maintenance page, then navigate to reconfigure
                    var maintenancePage = new MaintenanceModePage(mockInstall, this);
                    ContentFrame.Navigate(maintenancePage);
                    HideStandardNavigation();
                    break;

                case DemoScenario.Repair:
                    // Show maintenance page focused on repair
                    var maintenancePageRepair = new MaintenanceModePage(mockInstall, this);
                    ContentFrame.Navigate(maintenancePageRepair);
                    HideStandardNavigation();
                    break;

                case DemoScenario.Uninstall:
                    // Show uninstall page with mock data
                    var uninstallPage = new UninstallPage();
                    uninstallPage.SetInstallationToUninstall(mockInstall);
                    ContentFrame.Navigate(uninstallPage);
                    HideStandardNavigation();
                    break;

                default:
                    // Fallback to normal flow (fresh install wizard)
                    InitializeWizard();
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error launching demo scenario:\n\n{ex.Message}",
                "Demo Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void HideStandardNavigation()
    {
        BackButton.Visibility = Visibility.Collapsed;
        NextButton.Visibility = Visibility.Collapsed;
        CancelButton.Visibility = Visibility.Collapsed;
        StepIndicator.Visibility = Visibility.Collapsed;
    }

    private void ShowStandardNavigation()
    {
        BackButton.Visibility = Visibility.Visible;
        NextButton.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
        StepIndicator.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Navigate back to instance selection (main window)
    /// </summary>
    public async void ShowInstanceSelection()
    {
        try
        {
            // Re-detect all installations
            var upgradeDetection = new UpgradeDetectionService();
            var existingInstallations = await upgradeDetection.DetectAllInstallationsAsync();

            if (existingInstallations.Count > 0)
            {
                // Show instance selection page
                var instanceSelectionPage = new InstanceSelectionPage(existingInstallations, this);
                ContentFrame.Navigate(instanceSelectionPage);

                // Hide standard wizard navigation
                HideStandardNavigation();

                // Reset reconfigure mode
                _isReconfigureMode = false;
            }
            else
            {
                // No instances found - show fresh install wizard
                ShowStandardNavigation();
                InitializeWizard();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error loading instance selection:\n\n{ex.Message}",
                "Navigation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Save reconfiguration changes to the existing installation
    /// </summary>
    private async System.Threading.Tasks.Task<ReconfigureResult> SaveReconfigurationAsync(InstallationConfig config)
    {
        var result = new ReconfigureResult();
        // Read current installation details from registry to locate paths
        var installationService = new InstallationService();
        var reg = await installationService.ReadConfigurationFromRegistryAsync(config.SiteName);

        // Determine install path and DB values
        var installPath = reg.TryGetValue("InstallPath", out var p) ? p : config.InstallationPath;
        var dbServer = string.IsNullOrWhiteSpace(config.DatabaseServer) && reg.TryGetValue("DatabaseServer", out var rs)
            ? rs : config.DatabaseServer;
        var dbName = string.IsNullOrWhiteSpace(config.DatabaseName) && reg.TryGetValue("DatabaseName", out var rn)
            ? rn : config.DatabaseName;

        // 1) Note: Configuration is now registry-only (no appsettings.json updates)
        //    Connection string will be written to encrypted registry during installation
        result.ConnectionAttempted = true;

        try
        {
            if (!string.IsNullOrWhiteSpace(installPath))
            {
                // Test connection using the provided credentials
                var conn = $"Server={dbServer};Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

                try
                {
                    // Direct connection test (no file writes)
                    using (var testConn = new SqlConnection(conn))
                    {
                        await testConn.OpenAsync();
                        result.ConnectionUpdated = true;
                        // Note: Connection string will be written to encrypted registry during installation
                        // No appsettings.json file is created or modified here
                    }
                }
                catch (Exception jex)
                {
                    result.ConnectionError = jex.Message;
                }
            }
        }
        catch (System.Exception ex)
        {
            result.ConnectionError = ex.Message;
        }

        // 2) If admin credentials provided, reset or create admin user
        if (!string.IsNullOrWhiteSpace(config.AdminEmail) && !string.IsNullOrWhiteSpace(config.AdminPassword))
        {
            try
            {
                result.AdminAttempted = true;
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<string>();
                var hashed = passwordHasher.HashPassword(config.AdminEmail, config.AdminPassword);
                var escapedEmail = config.AdminEmail.Replace("'", "''");
                var escapedHash = hashed.Replace("'", "''");

                var sql = $@"
SET NOCOUNT ON;
IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = '{escapedEmail}')
BEGIN
    UPDATE AspNetUsers SET PasswordHash = '{escapedHash}',
        SecurityStamp = NEWID(), ConcurrencyStamp = NEWID()
    WHERE Email = '{escapedEmail}';
END
ELSE
BEGIN
    DECLARE @UserId NVARCHAR(450) = CAST(NEWID() AS NVARCHAR(450));
    DECLARE @AdminRoleId NVARCHAR(450); SELECT @AdminRoleId = Id FROM AspNetRoles WHERE Name = 'Admin';
    IF (@AdminRoleId IS NULL) BEGIN
        SET @AdminRoleId = CAST(NEWID() AS NVARCHAR(450));
        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (@AdminRoleId, 'Admin', 'ADMIN', NEWID());
    END
    INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, CreatedAt)
    VALUES (@UserId, '{escapedEmail}', UPPER('{escapedEmail}'), '{escapedEmail}', UPPER('{escapedEmail}'),
        1, '{escapedHash}', NEWID(), NEWID(), 0, 0, 1, 0, GETUTCDATE());
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @AdminRoleId);
END
";

                var temp = System.IO.Path.GetTempFileName() + ".sql";
                await System.IO.File.WriteAllTextAsync(temp, sql);

                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sqlcmd",
                        Arguments = $"-S \"{dbServer}\" -d \"{dbName}\" -E -i \"{temp}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var proc = System.Diagnostics.Process.Start(psi);
                    if (proc != null)
                    {
                        await proc.WaitForExitAsync();
                        if (proc.ExitCode != 0)
                        {
                            var err = await proc.StandardError.ReadToEndAsync();
                            result.AdminError = string.IsNullOrWhiteSpace(err) ? "sqlcmd returned non-zero exit code." : err.Trim();
                        }
                        else
                        {
                            result.AdminUpdated = true;
                        }
                    }
                    else
                    {
                        result.AdminError = "Failed to start sqlcmd process.";
                    }
                }
                catch (System.ComponentModel.Win32Exception w32ex)
                {
                    result.AdminError = $"sqlcmd not found: {w32ex.Message}";
                }
                finally
                {
                    try { System.IO.File.Delete(temp); } catch { }
                }
            }
            catch (System.Exception ex)
            {
                result.AdminError = ex.Message;
            }
        }

        // 3) Update registry values for display
        try
        {
            var registryPath = $@"SOFTWARE\\EcommerceStarter\\{config.SiteName}";
            using (var key = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(registryPath))
            {
                if (key != null)
                {
                    if (!string.IsNullOrWhiteSpace(config.CompanyName))
                        key.SetValue("CompanyName", config.CompanyName);
                    if (!string.IsNullOrWhiteSpace(config.AdminEmail))
                        key.SetValue("AdminEmail", config.AdminEmail);
                    // NOTE: DatabaseServer and DatabaseName are stored in the encrypted ConnectionStringEncrypted value
                    // No need to store them in plain text
                    result.RegistryUpdated = true;
                }
                else
                {
                    result.RegistryError = "Failed to open registry key.";
                }
            }
        }
        catch (Exception rex)
        {
            result.RegistryError = rex.Message;
        }

        // 4) Regenerate web.config with updated configuration (including APP_POOL_ID)
        try
        {
            result.WebConfigAttempted = true;
            await installationService.ApplyConfigurationAsync(config);
            result.WebConfigUpdated = true;
        }
        catch (Exception wcex)
        {
            result.WebConfigError = wcex.Message;
        }

        return result;
    }
}

internal class ReconfigureResult
{
    public bool ConnectionAttempted { get; set; }
    public bool ConnectionUpdated { get; set; }
    public string? ConnectionError { get; set; }
    public bool AdminAttempted { get; set; }
    public bool AdminUpdated { get; set; }
    public string? AdminError { get; set; }
    public bool RegistryUpdated { get; set; }
    public string? RegistryError { get; set; }
    public bool WebConfigAttempted { get; set; }
    public bool WebConfigUpdated { get; set; }
    public string? WebConfigError { get; set; }
}