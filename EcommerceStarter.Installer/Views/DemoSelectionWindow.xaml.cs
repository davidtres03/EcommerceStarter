using System;
using System.Windows;
using EcommerceStarter.Installer.Models;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Demo mode selection window
/// Allows user to choose which installer scenario to demonstrate
/// </summary>
public partial class DemoSelectionWindow : Window
{
    public DemoSelectionWindow()
    {
        InitializeComponent();
    }

    private void FreshInstall_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.FreshInstall);
    }

    private void Reconfigure_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.Reconfigure);
    }

    private void Repair_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.Repair);
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        LaunchDemo(DemoScenario.Uninstall);
    }

    private void LaunchDemo(DemoScenario scenario)
    {
        try
        {
            // Set the current demo scenario in App
            App.IsDemoMode = true;

            // Create and show main window with demo scenario
            var mainWindow = new MainWindow();
            mainWindow.LaunchDemoScenario(scenario);
            mainWindow.Show();

            // Close selection window
            Close();
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

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
