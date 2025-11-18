using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class UninstallProgressPage : Page
{
    private readonly UninstallService _uninstallService;

    public UninstallProgressPage()
    {
        InitializeComponent();
        _uninstallService = new UninstallService();
    }

    public async Task StartUninstallAsync(UninstallOptions options)
    {
        try
        {
            var progress = new Progress<UninstallProgress>(p =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = p.Percentage;
                    StatusMessage.Text = p.Message;
                    CurrentStepText.Text = p.CurrentStep;
                });
            });

            var result = await _uninstallService.UninstallAsync(options, progress);

            Dispatcher.Invoke(() =>
            {
                ShowResult(result);
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                ShowError(ex.Message);
            });
        }
    }

    private void ShowResult(UninstallResult result)
    {
        ResultPanel.Visibility = Visibility.Visible;
        CloseButton.Visibility = Visibility.Visible;

        if (result.Success)
        {
            // Success
            ResultIcon.Text = "\u2713"; // ? checkmark
            ResultIcon.Foreground = new SolidColorBrush(Color.FromRgb(75, 181, 67)); // Green
            ResultMessage.Text = result.Message;
            ResultPanel.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218)); // Light green
            ResultPanel.BorderBrush = new SolidColorBrush(Color.FromRgb(75, 181, 67)); // Green

            if (result.Warnings.Count > 0)
            {
                ResultMessage.Text += "\n\nWarnings:\n" + string.Join("\n", result.Warnings);
            }
        }
        else
        {
            // Error
            ResultIcon.Text = "\u274C"; // ? cross
            ResultIcon.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
            ResultMessage.Text = $"Uninstallation failed:\n{result.ErrorMessage}";
            ResultPanel.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218)); // Light red
            ResultPanel.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
        }
    }

    private void ShowError(string errorMessage)
    {
        ResultPanel.Visibility = Visibility.Visible;
        CloseButton.Visibility = Visibility.Visible;

        ResultIcon.Text = "\u274C"; // ? cross
        ResultIcon.Foreground = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
        ResultMessage.Text = $"An error occurred during uninstallation:\n{errorMessage}";
        ResultPanel.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218)); // Light red
        ResultPanel.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
