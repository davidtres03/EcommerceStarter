using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Models;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Beautiful demo launcher window
/// </summary>
public partial class DemoLauncherWindow : Window
{
    public DemoLauncherWindow()
    {
        InitializeComponent();
        
        // Production mode: We're running from the published EXE already!
        // No need to detect builds - just launch scenarios directly
        
        // Hide build selector in production
        HideBuildSelector();
    }

    private void HideBuildSelector()
    {
        // In production, we don't need build selection
        // The EXE is already built and running!
        var buildCard = FindName("BuildConfigCard") as Border;
        if (buildCard != null)
        {
            buildCard.Visibility = Visibility.Collapsed;
        }
    }

    private void LaunchFreshInstall_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.FreshInstall);
    }

    private void LaunchUpgrade_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.Upgrade);
    }

    private void LaunchReconfigure_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.Reconfigure);
    }

    private void LaunchUninstall_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.Uninstall);
    }

    private void LaunchDemo(DemoScenario scenario)
    {
        try
        {
            // Production: We're already running from the EXE
            // Just set demo mode and launch the scenario in this process!
            App.IsDemoMode = true;
            App.CurrentDemoScenario = scenario;
            
            // Create main window with demo scenario
            var mainWindow = new MainWindow();
            mainWindow.LaunchDemoScenario(scenario);
            mainWindow.Show();
            
            // Close demo launcher
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error launching demo:\n\n{ex.Message}",
                "Launch Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
