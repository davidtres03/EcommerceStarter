using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Maintenance mode page for existing installations
/// Provides options to upgrade, reconfigure, repair, or uninstall
/// </summary>
public partial class MaintenanceModePage : Page
{
    private readonly ExistingInstallation _existingInstall;
    private readonly MainWindow _mainWindow;
    
    public MaintenanceModePage(ExistingInstallation existingInstall, MainWindow mainWindow)
    {
        InitializeComponent();
        _existingInstall = existingInstall ?? throw new ArgumentNullException(nameof(existingInstall));
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        
        Loaded += MaintenanceModePage_Loaded;
    }
    
    private void MaintenanceModePage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadInstallationInfo();
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
                HealthStatusText.Text = "✅ Installation is healthy";
                HealthStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0D5E4F"));
            }
            else if (_existingInstall.Issues != null && _existingInstall.Issues.Any())
            {
                HealthStatusBorder.Visibility = Visibility.Visible;
                HealthStatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3CD"));
                HealthStatusBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
                HealthStatusText.Text = "?? Issues detected:";
                HealthStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#856404"));
                
                IssuesText.Text = string.Join("\n• ", _existingInstall.Issues.Select(i => i));
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
    
    private void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Navigate to upgrade welcome page
            var upgradePage = new UpgradeWelcomePage(_existingInstall);
            NavigationService?.Navigate(upgradePage);
        }
        catch (Exception ex)
        {
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
                "• Reset the admin password\n" +
                "• Update company information\n" +
                "• Change Stripe/email settings\n\n" +
                "The application files and database will NOT be modified.",
                "Confirm Reconfigure",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Navigate to configuration page in reconfigure mode
                var configPage = new ConfigurationPage();
                
                // Load existing configuration from the installation
                configPage.LoadExistingConfiguration(_existingInstall);
                
                NavigationService?.Navigate(configPage);
                
                // Update main window to show reconfigure mode
                _mainWindow.SetReconfigureMode(true);
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
                "• Verify all application files\n" +
                "• Replace missing or corrupted files\n" +
                "• Repair IIS configuration\n" +
                "• Validate database connection\n\n" +
                "Your data will NOT be modified.",
                "Confirm Repair",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // TODO: Navigate to repair page when implemented
                MessageBox.Show(
                    "Repair functionality coming soon!\n\n" +
                    "For now, you can:\n" +
                    "• Run the installer to upgrade\n" +
                    "• Manually verify files in the install directory\n" +
                    "• Check IIS configuration",
                    "Repair",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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
                $"?? UNINSTALL {_existingInstall.CompanyName}?\n\n" +
                $"This will REMOVE:\n" +
                $"• Application files from {_existingInstall.InstallPath}\n" +
                $"• IIS website and app pool\n" +
                $"• Windows registry entries\n\n" +
                $"The DATABASE will be preserved:\n" +
                $"• {_existingInstall.DatabaseName} will NOT be deleted\n" +
                $"• {_existingInstall.ProductCount} products will remain\n" +
                $"• {_existingInstall.OrderCount} orders will remain\n\n" +
                $"Are you SURE you want to uninstall?",
                "?? Confirm Uninstall",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
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
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Confirm exit
            var result = MessageBox.Show(
                "Exit installer?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
        catch (Exception ex)
        {
            // Even if there's an error, allow shutdown
            Application.Current.Shutdown();
        }
    }
}
