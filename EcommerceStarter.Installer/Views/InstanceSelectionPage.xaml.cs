using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EcommerceStarter.Installer.Services;
using System.Diagnostics;

namespace EcommerceStarter.Installer.Views;

/// <summary>
/// Instance selection page - shows all existing instances and allows selecting one or installing new
/// </summary>
public partial class InstanceSelectionPage : Page
{
    private readonly List<ExistingInstallation> _installations;
    private readonly MainWindow _mainWindow;

    public InstanceSelectionPage(List<ExistingInstallation> installations, MainWindow mainWindow)
    {
        InitializeComponent();
        _installations = installations ?? throw new ArgumentNullException(nameof(installations));
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

        // Bind installations to UI
        InstancesList.ItemsSource = _installations;
    }

    private void Instance_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is ExistingInstallation installation)
        {
            // Navigate to maintenance mode for selected instance
            var maintenancePage = new MaintenanceModePage(installation, _mainWindow);
            NavigationService?.Navigate(maintenancePage);
        }
    }

    private void NewInstance_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // User wants to install a new instance - launch full wizard
        _mainWindow.StartNewInstallationWizard();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OpenSite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExistingInstallation inst)
        {
            try
            {
                var url = !string.IsNullOrWhiteSpace(inst.WebAppUrl)
                    ? inst.WebAppUrl
                    : (inst.LocalhostPort > 0 ? $"http://localhost:{inst.LocalhostPort}" : "http://localhost");
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open site: {ex.Message}", "Open Site", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExistingInstallation inst)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(inst.InstallPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = inst.InstallPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Open Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void Reconfigure_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExistingInstallation inst)
        {
            // Navigate to maintenance page and trigger reconfigure
            var maintenancePage = new MaintenanceModePage(inst, _mainWindow);
            NavigationService?.Navigate(maintenancePage);
        }
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ExistingInstallation inst)
        {
            var uninstallPage = new UninstallPage();
            uninstallPage.SetInstallationToUninstall(inst);
            NavigationService?.Navigate(uninstallPage);
        }
    }
}
