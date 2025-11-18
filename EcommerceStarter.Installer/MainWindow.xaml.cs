using System;
using System.Collections.Generic;
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
using EcommerceStarter.Installer.Views;
using EcommerceStarter.Installer.Models;
using EcommerceStarter.Installer.Services;

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

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[MainWindow] === MAINWINDOW LOADED ===");

            // Check for existing installation FIRST (before initializing wizard pages)
            System.Diagnostics.Debug.WriteLine("[MainWindow] Creating UpgradeDetectionService...");
            var upgradeDetection = new UpgradeDetectionService();

            System.Diagnostics.Debug.WriteLine("[MainWindow] Calling DetectExistingInstallationAsync...");
            var existingInstall = await upgradeDetection.DetectExistingInstallationAsync();

            if (existingInstall != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] Existing installation detected: {existingInstall.SiteName} v{existingInstall.Version}");

                // EXISTING INSTALLATION FOUND - Show Maintenance Mode Page
                var maintenancePage = new MaintenanceModePage(existingInstall, this);
                ContentFrame.Navigate(maintenancePage);

                // Hide standard wizard navigation for maintenance mode
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
                StepIndicator.Visibility = Visibility.Collapsed;

                return; // Exit early - maintenance mode handles its own navigation
            }

            System.Diagnostics.Debug.WriteLine("[MainWindow] No existing installation found - showing fresh install wizard");

            // NO EXISTING INSTALLATION - Initialize wizard pages and show fresh install wizard
            InitializeWizard();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindow] ERROR in MainWindow_Loaded: {ex.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");

            MessageBox.Show(
                $"Error detecting existing installation:\n\n{ex.GetType().Name}: {ex.Message}\n\nProceeding with fresh installation wizard.",
                "Detection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Initialize wizard and continue with fresh install
            InitializeWizard();
        }
    }

    private void InitializeWizard()
    {
        // Create wizard pages in order
        // UpdateCheckPage comes FIRST - let user see available updates
        _pages.Add(new UpdateCheckPage());
        _pages.Add(new WelcomePage());
        _pages.Add(new PrerequisitesPage());
        _pages.Add(new ConfigurationPage());
        _pages.Add(new InstallationPage());
        _pages.Add(new CompletionPage());

        // Navigate to first page
        NavigateToPage(0);
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
}