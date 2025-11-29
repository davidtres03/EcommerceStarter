using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Models;

namespace EcommerceStarter.Installer.Views;

public partial class CompletionPage : Page
{
    private InstallationConfig? _config;
    
    public CompletionPage()
    {
        InitializeComponent();
    }
    
    public void SetConfiguration(InstallationConfig config)
    {
        _config = config;
        
        System.Diagnostics.Debug.WriteLine($"[CompletionPage] SetConfiguration called:");
        System.Diagnostics.Debug.WriteLine($"  - Admin Email: {config.AdminEmail}");
        System.Diagnostics.Debug.WriteLine($"  - Admin Password: {(config.AdminPassword?.Length > 0 ? $"[{config.AdminPassword.Length} chars]" : "EMPTY/NULL")}");
        System.Diagnostics.Debug.WriteLine($"  - Domain: {config.Domain}");
        
        // Display credentials
        AdminEmailText.Text = config.AdminEmail;
        AdminPasswordText.Text = config.AdminPassword;
        
        System.Diagnostics.Debug.WriteLine($"[CompletionPage] AdminPasswordText.Text set to: '{AdminPasswordText.Text}'");
        
        // Set URLs - use HTTP since SSL is not configured by the installer
        // The application is created under Default Web Site on port 80
        var baseUrl = $"http://{config.Domain}/{config.SiteName}";
            
        StorefrontUrlText.Text = baseUrl;
        AdminUrlText.Text = $"{baseUrl}/Admin";
    }
    
    private void CopyEmail_Click(object sender, RoutedEventArgs e)
    {
        if (_config != null)
        {
            Clipboard.SetText(_config.AdminEmail);
            ShowCopyFeedback((Button)sender, "Copied!");
        }
    }
    
    private void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        if (_config != null)
        {
            Clipboard.SetText(_config.AdminPassword);
            ShowCopyFeedback((Button)sender, "Copied!");
        }
    }
    
    private async void ShowCopyFeedback(Button button, string message)
    {
        var originalContent = button.Content;
        button.Content = message;
        button.IsEnabled = false;
        
        await Task.Delay(1500);
        
        button.Content = originalContent;
        button.IsEnabled = true;
    }
    
    private void OpenStorefront_Click(object sender, RoutedEventArgs e)
    {
        if (_config != null)
        {
            var url = $"http://{_config.Domain}/{_config.SiteName}";
                
            OpenUrl(url);
        }
    }
    
    private void OpenAdmin_Click(object sender, RoutedEventArgs e)
    {
        if (_config != null)
        {
            var url = $"http://{_config.Domain}/{_config.SiteName}/Admin";
                
            OpenUrl(url);
        }
    }
    
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open browser: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
