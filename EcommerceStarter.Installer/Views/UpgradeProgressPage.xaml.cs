using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class UpgradeProgressPage : Page
{
    private readonly ExistingInstallation _existingInstall;
    private readonly UpgradeService _upgradeService;
    private string? _downloadedZipPath;
    
    public UpgradeProgressPage(ExistingInstallation existingInstall)
    {
        InitializeComponent();
        _existingInstall = existingInstall;
        _upgradeService = new UpgradeService();
        
        // Wire up events
        _upgradeService.ProgressUpdate += OnProgressUpdate;
        _upgradeService.StatusUpdate += OnStatusUpdate;
        
        // Start upgrade process
        Loaded += async (s, e) => await StartUpgradeAsync();
    }
    
    private async Task StartUpgradeAsync()
    {
        try
        {
            // Step 1: Check for application updates from GitHub
            StatusText.Text = "Checking for updates...";
            
            var updateService = new UpdateService();
            var appUpdate = await updateService.CheckForApplicationUpdatesAsync();
            
            if (appUpdate == null)
            {
                ShowError("No updates found on GitHub. Please create a release first.");
                return;
            }
            
            // Step 2: Download update
            StatusText.Text = $"Downloading v{appUpdate.Version}...";
            ProgressBar.IsIndeterminate = true;
            
            var downloadProgress = new Progress<int>(percent =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = percent;
                    StatusText.Text = $"Downloading... {percent}%";
                });
            });
            
            _downloadedZipPath = await updateService.DownloadApplicationUpdateAsync(appUpdate.DownloadUrl, downloadProgress);
            
            if (string.IsNullOrEmpty(_downloadedZipPath))
            {
                ShowError("Failed to download update from GitHub.");
                return;
            }
            
            // Step 3: Perform upgrade
            ProgressBar.Value = 0;
            ProgressBar.IsIndeterminate = false;
            StatusText.Text = "Starting upgrade...";
            
            var result = await _upgradeService.UpgradeFromZipAsync(_existingInstall, _downloadedZipPath);
            
            if (result.Success)
            {
                ShowSuccess(result);
            }
            else
            {
                ShowError(result.ErrorMessage, result.BackupPath);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Upgrade failed: {ex.Message}");
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
            DetailText.Text += $"\n{status}";
        });
    }
    
    private void ShowSuccess(UpgradeResult result)
    {
        Dispatcher.Invoke(() =>
        {
            ResultPanel.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
            
            ResultIcon.Text = "?";
            ResultIcon.Foreground = System.Windows.Media.Brushes.Green;
            ResultTitle.Text = "Upgrade Successful!";
            ResultMessage.Text = result.Message;
            
            if (!string.IsNullOrEmpty(result.BackupPath))
            {
                ResultMessage.Text += $"\n\nBackup created at:\n{result.BackupPath}";
            }
        });
    }
    
    private void ShowError(string errorMessage, string? backupPath = null)
    {
        Dispatcher.Invoke(() =>
        {
            ResultPanel.Visibility = Visibility.Visible;
            ProgressPanel.Visibility = Visibility.Collapsed;
            
            ResultIcon.Text = "?";
            ResultIcon.Foreground = System.Windows.Media.Brushes.Red;
            ResultTitle.Text = "Upgrade Failed";
            ResultMessage.Text = errorMessage;
            
            if (!string.IsNullOrEmpty(backupPath))
            {
                ResultMessage.Text += $"\n\nBackup available at:\n{backupPath}";
            }
        });
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
