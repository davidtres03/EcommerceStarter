using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class PrerequisitesPage : Page
{
    private readonly PrerequisiteService _service = new();
    private bool _allPrerequisitesMet = false;
    
    public PrerequisitesPage()
    {
        InitializeComponent();
        
        _service.StatusUpdate += (s, message) =>
        {
            Dispatcher.Invoke(() => StatusText.Text = message);
        };
        
        _service.ProgressUpdate += (s, progress) =>
        {
            Dispatcher.Invoke(() => InstallProgress.Value = progress);
        };
    }
    
    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckPrerequisitesAsync();
    }
    
    private async Task CheckPrerequisitesAsync()
    {
        // Get system info
        var sysInfo = await _service.GetSystemInfoAsync();
        
        OSInfo.Text = $"Operating System: {sysInfo.OperatingSystem}";
        DiskSpace.Text = $"Free Disk Space: {sysInfo.FreeSpaceGB:F1} GB";
        
        if (sysInfo.IsAdministrator)
        {
            AdminStatus.Text = "Administrator: \u2713 Yes";  // ? checkmark
        }
        else
        {
            AdminStatus.Text = "Administrator: \u2717 No - Some features may require admin";  // ? X mark
        }
        
        // Check .NET
        StatusText.Text = "Checking .NET 8+ SDK...";
        var hasDotNet = await _service.IsDotNetInstalledAsync();
        UpdateStatus(DotNetStatus, DotNetMessage, InstallDotNetButton, hasDotNet, ".NET 8 or higher is installed", "Not installed");
        
        // Check SQL Server
        StatusText.Text = "Checking SQL Server...";
        var hasSql = await _service.IsSqlServerInstalledAsync();
        if (hasSql)
        {
            var sqlRunning = await _service.IsSqlServerRunningAsync();
            UpdateStatus(SqlStatus, SqlMessage, InstallSqlButton, true, 
                sqlRunning ? "SQL Server is running" : "SQL Server installed but not running", "");
        }
        else
        {
            UpdateStatus(SqlStatus, SqlMessage, InstallSqlButton, false, "", "Not installed");
        }
        
        // Check IIS
        StatusText.Text = "Checking IIS...";
        var hasIIS = await _service.IsIISInstalledAsync();
        UpdateStatus(IISStatus, IISMessage, InstallIISButton, hasIIS, "IIS is installed", "Not installed");
        
        // Check ASP.NET Core Hosting Bundle
        StatusText.Text = "Checking ASP.NET Core Hosting Bundle...";
        var hasHostingBundle = await _service.IsHostingBundleInstalledAsync();
        UpdateStatus(HostingBundleStatus, HostingBundleMessage, InstallHostingBundleButton, hasHostingBundle, 
            "ASP.NET Core Hosting Bundle is installed", "Not installed");
        
        // Final status
        _allPrerequisitesMet = hasDotNet && hasSql && hasIIS && hasHostingBundle;
        
        if (_allPrerequisitesMet)
        {
            StatusText.Text = "\u2713 All prerequisites are installed! Click Next to continue.";  // ?
            StatusText.Foreground = (System.Windows.Media.Brush)FindResource("BrandSuccessBrush");
        }
        else
        {
            StatusText.Text = "Some components are missing. Click Install buttons to install them.";
        }
    }
    
    private void UpdateStatus(TextBlock statusIcon, TextBlock message, Button button, 
                             bool isInstalled, string installedMsg, string notInstalledMsg)
    {
        if (isInstalled)
        {
            statusIcon.Text = "\u2713";  // ? checkmark
            statusIcon.Foreground = (System.Windows.Media.Brush)FindResource("BrandSuccessBrush");
            message.Text = installedMsg;
            button.Visibility = Visibility.Collapsed;
        }
        else
        {
            statusIcon.Text = "\u2717";  // ? X mark
            statusIcon.Foreground = (System.Windows.Media.Brush)FindResource("BrandDangerBrush");
            message.Text = notInstalledMsg;
            button.Visibility = Visibility.Visible;
        }
    }
    
    private async void InstallDotNet_Click(object sender, RoutedEventArgs e)
    {
        if (App.IsDebugMode)
        {
            // Show mock UAC dialog
            var uacDialog = new MockUacDialog(".NET 8 SDK Installer", "Microsoft Corporation");
            if (uacDialog.ShowDialog() == true)
            {
                // Show mock installation progress
                var installDialog = new MockInstallDialog(".NET 8 SDK", "Installing .NET 8 SDK...");
                installDialog.ShowDialog();
                
                // Show success dialog
                var successDialog = new MockSuccessDialog(".NET 8 SDK installed successfully!");
                successDialog.ShowDialog();
                
                // Refresh the prerequisites check
                await CheckPrerequisitesAsync();
            }
            return;
        }
        
        InstallDotNetButton.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        InstallProgress.IsIndeterminate = false;
        
        var success = await _service.InstallDotNetAsync();
        
        InstallProgress.Visibility = Visibility.Collapsed;
        
        if (success)
        {
            await CheckPrerequisitesAsync();
        }
        else
        {
            MessageBox.Show("Failed to install .NET 8 SDK. Please try installing manually.", 
                          "Installation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            InstallDotNetButton.IsEnabled = true;
        }
    }
    
    private async void InstallSql_Click(object sender, RoutedEventArgs e)
    {
        if (App.IsDebugMode)
        {
            // Show mock UAC dialog
            var uacDialog = new MockUacDialog("SQL Server 2022 Express", "Microsoft Corporation");
            if (uacDialog.ShowDialog() == true)
            {
                // Show mock installation progress
                var installDialog = new MockInstallDialog("SQL Server 2022 Express", "Installing SQL Server Express...");
                installDialog.ShowDialog();
                
                // Show success dialog
                var successDialog = new MockSuccessDialog("SQL Server Express installed successfully!");
                successDialog.ShowDialog();
                
                // Refresh the prerequisites check
                await CheckPrerequisitesAsync();
            }
            return;
        }
        
        InstallSqlButton.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        InstallProgress.IsIndeterminate = true;
        
        var success = await _service.InstallSqlServerAsync();
        
        InstallProgress.Visibility = Visibility.Collapsed;
        
        if (success)
        {
            await CheckPrerequisitesAsync();
        }
        else
        {
            MessageBox.Show("Failed to install SQL Server. Please try installing manually.", 
                          "Installation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            InstallSqlButton.IsEnabled = true;
        }
    }
    
    private async void InstallIIS_Click(object sender, RoutedEventArgs e)
    {
        if (App.IsDebugMode)
        {
            // Show mock UAC dialog
            var uacDialog = new MockUacDialog("Windows Features", "Microsoft Windows");
            if (uacDialog.ShowDialog() == true)
            {
                // Show mock installation progress
                var installDialog = new MockInstallDialog("IIS (Internet Information Services)", "Enabling Windows features...");
                installDialog.ShowDialog();
                
                // Show success dialog
                var successDialog = new MockSuccessDialog("IIS installed successfully!");
                successDialog.ShowDialog();
                
                // Refresh the prerequisites check
                await CheckPrerequisitesAsync();
            }
            return;
        }
        
        InstallIISButton.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        InstallProgress.IsIndeterminate = true;
        
        var success = await _service.InstallIISAsync();
        
        InstallProgress.Visibility = Visibility.Collapsed;
        
        if (success)
        {
            await CheckPrerequisitesAsync();
        }
        else
        {
            MessageBox.Show("Failed to install IIS. Please try installing manually.", 
                          "Installation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            InstallIISButton.IsEnabled = true;
        }
    }
    
    private async void InstallHostingBundle_Click(object sender, RoutedEventArgs e)
    {
        if (App.IsDebugMode)
        {
            // Show mock UAC dialog
            var uacDialog = new MockUacDialog("ASP.NET Core Hosting Bundle", "Microsoft Corporation");
            if (uacDialog.ShowDialog() == true)
            {
                // Show mock installation progress
                var installDialog = new MockInstallDialog("ASP.NET Core Hosting Bundle", "Installing hosting bundle...");
                installDialog.ShowDialog();
                
                // Show success dialog
                var successDialog = new MockSuccessDialog("ASP.NET Core Hosting Bundle installed successfully!");
                successDialog.ShowDialog();
                
                // Refresh the prerequisites check
                await CheckPrerequisitesAsync();
            }
            return;
        }
        
        InstallHostingBundleButton.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        InstallProgress.IsIndeterminate = true;
        
        var success = await _service.InstallHostingBundleAsync();
        
        InstallProgress.Visibility = Visibility.Collapsed;
        
        if (success)
        {
            await CheckPrerequisitesAsync();
        }
        else
        {
            MessageBox.Show(
                "Failed to install ASP.NET Core Hosting Bundle automatically.\n\n" +
                "The download page has been opened in your browser if available.\n\n" +
                "Please download and install the 'Hosting Bundle' for Windows, then click the Install button again or restart the installer.",
                "Installation Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            InstallHostingBundleButton.IsEnabled = true;
        }
    }
}
