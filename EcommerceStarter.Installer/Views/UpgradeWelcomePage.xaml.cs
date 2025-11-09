using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class UpgradeWelcomePage : Page
{
    private readonly ExistingInstallation _existingInstall;
    
    public UpgradeWelcomePage(ExistingInstallation existingInstall)
    {
        InitializeComponent();
        _existingInstall = existingInstall;
        LoadInstallationInfo();
    }
    
    private void LoadInstallationInfo()
    {
        // Display installation info
        CompanyNameText.Text = _existingInstall.CompanyName;
        InstallPathText.Text = _existingInstall.InstallPath;
        DatabaseText.Text = $"{_existingInstall.DatabaseServer} / {_existingInstall.DatabaseName}";
        ProductCountText.Text = _existingInstall.ProductCount.ToString();
        OrderCountText.Text = _existingInstall.OrderCount.ToString();
        UserCountText.Text = _existingInstall.UserCount.ToString();
        CurrentVersionText.Text = _existingInstall.Version;
        NewVersionText.Text = "1.0.0"; // TODO: Get from assembly version
        
        // Show health status
        if (_existingInstall.IsHealthy)
        {
            HealthStatusText.Text = "? Installation is healthy";
            HealthStatusText.Foreground = System.Windows.Media.Brushes.Green;
        }
        else
        {
            HealthStatusText.Text = "? Issues detected:";
            HealthStatusText.Foreground = System.Windows.Media.Brushes.Orange;
            IssuesText.Text = string.Join("\n", _existingInstall.Issues);
            IssuesText.Visibility = Visibility.Visible;
        }
    }
    
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
    
    private void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to upgrade progress page
        var progressPage = new UpgradeProgressPage(_existingInstall);
        NavigationService?.Navigate(progressPage);
    }
}
