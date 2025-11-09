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
            // Check for existing installation
            var upgradeDetection = new UpgradeDetectionService();
            var existingInstall = await upgradeDetection.DetectExistingInstallationAsync();
            
            if (existingInstall != null)
            {
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
            
            // NO EXISTING INSTALLATION - Initialize Fresh Install Wizard
            InitializeWizard();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error detecting existing installation:\n\n{ex.Message}\n\nProceeding with fresh installation wizard.",
                "Detection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            
            // Fall back to fresh install wizard
            InitializeWizard();
        }
    }
    
    private void InitializeWizard()
    {
        // Create wizard pages in order
        _pages.Add(new WelcomePage());
        _pages.Add(new PrerequisitesPage());
        _pages.Add(new ConfigurationPage());
        _pages.Add(new InstallationPage());
        _pages.Add(new CompletionPage());
        
        // Navigate to first page
        NavigateToPage(0);
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
        if (_currentPageIndex == _pages.Count - 1)
        {
            // Finish - close the installer
            Application.Current.Shutdown();
        }
        else if (_currentPageIndex == _pages.Count - 2 && _pages[_currentPageIndex] is InstallationPage installPage)
        {
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
            // Validate current page before moving forward
            if (ValidateCurrentPage())
            {
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
        }
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
            return configPage.IsFormValid();
        }
        
        // Other pages - allow navigation
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
                CompanyName = "Cap & Collar Supply Co.",
                InstallPath = @"C:\inetpub\wwwroot\CapAndCollarSupplyCo",
                DatabaseServer = "localhost\\SQLEXPRESS",
                DatabaseName = "CapAndCollarSupplyCo",
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