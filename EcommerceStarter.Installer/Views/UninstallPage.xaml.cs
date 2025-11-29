using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class UninstallPage : Page
{
    private readonly InstallationStateService _stateService;
    private InstallationInfo? _installInfo;
    private System.Collections.Generic.List<InstallationInfo> _instances = new();

    public UninstallPage()
    {
        InitializeComponent();
        _stateService = new InstallationStateService();
        LoadInstallationInfo();
    }

    private void LoadInstallationInfo()
    {
        // Load all instances for picker
        _instances = _stateService.GetInstallations();
        if (_instances != null && _instances.Count > 0)
        {
            var pickerPanel = this.FindName("InstancePickerPanel") as FrameworkElement;
            if (pickerPanel != null) pickerPanel.Visibility = Visibility.Visible;

            var combo = this.FindName("InstanceCombo") as ComboBox;
            if (combo != null)
            {
                combo.Items.Clear();
                foreach (var inst in _instances)
                {
                    combo.Items.Add(new ComboBoxItem
                    {
                        Content = string.IsNullOrWhiteSpace(inst.SiteName) ? inst.InstallPath : inst.SiteName,
                        Tag = inst
                    });
                }
                combo.SelectedIndex = 0;
            }

            _installInfo = _instances[0];
        }
        else
        {
            _installInfo = _stateService.GetInstallationInfo();
        }
        
        if (_installInfo != null)
        {
            var siteNameText = this.FindName("SiteNameText") as TextBlock; if (siteNameText != null)
            {
                siteNameText.Text = _installInfo.SiteName ?? "(unknown)";
            }
            VersionText.Text = _installInfo.Version;
            LocationText.Text = _installInfo.InstallPath;
            InstallDateText.Text = _installInfo.InstallDate;
        }
        else
        {
            // No installation found
            VersionText.Text = "Not found";
            LocationText.Text = "Not found";
            InstallDateText.Text = "Not found";
            UninstallButton.IsEnabled = false;
        }
    }

    private void RemoveDatabase_Checked(object sender, RoutedEventArgs e)
    {
        if (DatabaseWarning != null && DatabaseRemovalText != null)
        {
            DatabaseWarning.Visibility = Visibility.Visible;
            DatabaseRemovalText.Visibility = Visibility.Visible;
        }
    }

    private void RemoveDatabase_Unchecked(object sender, RoutedEventArgs e)
    {
        if (DatabaseWarning != null && DatabaseRemovalText != null)
        {
            DatabaseWarning.Visibility = Visibility.Collapsed;
            DatabaseRemovalText.Visibility = Visibility.Collapsed;
        }
    }

    private void KeepUserData_Checked(object sender, RoutedEventArgs e)
    {
        if (UserDataRemovalText != null)
        {
            UserDataRemovalText.Visibility = Visibility.Collapsed;
        }
    }

    private void KeepUserData_Unchecked(object sender, RoutedEventArgs e)
    {
        if (UserDataRemovalText != null)
        {
            UserDataRemovalText.Visibility = Visibility.Visible;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Close the window
        Window.GetWindow(this)?.Close();
    }

    private async void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        // ?? DEMO MODE CHECK - Don't actually uninstall!
        if (App.IsDemoMode)
        {
            MessageBox.Show(
                "?? DEMO MODE - No Real Changes\n\n" +
                "This is a demonstration of the uninstall process.\n\n" +
                "In demo mode:\n" +
                "• No files will be deleted\n" +
                "• No database will be removed\n" +
                "• No IIS sites will be modified\n" +
                "• No registry entries will be changed\n\n" +
                "Everything is safe!",
                "Demo Mode Active",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            // Navigate to progress page with demo mode
            var demoProgressPage = new UninstallProgressPage();
            var demoOptions = new UninstallOptions
            {
                RemoveDatabase = RemoveDatabaseCheckBox.IsChecked == true,
                KeepUserData = KeepUserDataCheckBox.IsChecked == true,
                DatabaseServer = "localhost\\SQLEXPRESS",
                DatabaseName = "DemoDatabase",
                SiteName = "DemoSite"
            };
            
            NavigationService?.Navigate(demoProgressPage);
            await demoProgressPage.StartUninstallAsync(demoOptions);
            return;
        }
        
        // Confirmation dialog
        var result = MessageBox.Show(
            "Are you sure you want to uninstall EcommerceStarter?\n\nThis action cannot be undone!",
            "Confirm Uninstallation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
            return;

        // Second confirmation if removing database
        if (RemoveDatabaseCheckBox.IsChecked == true)
        {
            var dbConfirm = MessageBox.Show(
                "WARNING: You are about to permanently delete the database!\n\n" +
                "All products, orders, customers, and other data will be lost forever.\n\n" +
                "Are you absolutely sure you want to continue?",
                "Confirm Database Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Stop,
                MessageBoxResult.No);

            if (dbConfirm != MessageBoxResult.Yes)
                return;
        }

        // Navigate to uninstall progress page
        var progressPage = new UninstallProgressPage();
        
        var options = new UninstallOptions
        {
            RemoveDatabase = RemoveDatabaseCheckBox.IsChecked == true,
            KeepUserData = KeepUserDataCheckBox.IsChecked == true,
            // Get database info from installation state if available
            DatabaseServer = _installInfo?.InstallPath != null ? ExtractDatabaseServer(_installInfo.InstallPath) : "localhost\\SQLEXPRESS",
            DatabaseName = _installInfo?.InstallPath != null ? ExtractDatabaseName(_installInfo.InstallPath) : "MyStore",
            SiteName = !string.IsNullOrWhiteSpace(_installInfo?.SiteName) ? _installInfo.SiteName : (_installInfo?.InstallPath != null ? System.IO.Path.GetFileName(_installInfo.InstallPath) : "EcommerceStarter")
        };

        NavigationService?.Navigate(progressPage);
        
        // Start uninstallation on the progress page
        await progressPage.StartUninstallAsync(options);
    }

    private void InstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is InstallationInfo inst)
        {
            _installInfo = inst;
            var siteNameText = this.FindName("SiteNameText") as TextBlock; if (siteNameText != null)
            {
                siteNameText.Text = _installInfo.SiteName ?? "(unknown)";
            }
            VersionText.Text = _installInfo.Version;
            LocationText.Text = _installInfo.InstallPath;
            InstallDateText.Text = _installInfo.InstallDate;
        }
    }

    private string ExtractDatabaseServer(string installPath)
    {
        // Try to read from web.config or appsettings.json
        // For now, use default
        return "localhost\\SQLEXPRESS";
    }

    private string ExtractDatabaseName(string installPath)
    {
        // Try to extract from path or config
        // Use site name as database name by default
        var siteName = System.IO.Path.GetFileName(installPath);
        return siteName ?? "MyStore";
    }
    
    /// <summary>
    /// Set the installation to uninstall (called from MaintenanceModePage)
    /// </summary>
    public void SetInstallationToUninstall(ExistingInstallation existingInstall)
    {
        if (existingInstall == null) return;
        
        _installInfo = new InstallationInfo
        {
            InstallPath = existingInstall.InstallPath,
            Version = existingInstall.Version,
            InstallDate = System.DateTime.Now.ToString("yyyy-MM-dd") // Approximate
        };
        
        // Update UI with installation details
        var detailsText = this.FindName("InstallationDetailsText") as TextBlock; if (detailsText != null)
        {
            detailsText.Text = $"Store: {existingInstall.CompanyName}\n" +
                                          $"Location: {existingInstall.InstallPath}\n" +
                                          $"Database: {existingInstall.DatabaseName}\n" +
                                          $"Version: {existingInstall.Version}";
        }
        
        System.Diagnostics.Debug.WriteLine($"[UninstallPage] Set installation to uninstall: {existingInstall.CompanyName}");
    }
}
