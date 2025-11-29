using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class WelcomePage : Page
{
    public bool IsReconfiguration { get; private set; }
    public string? ExistingInstallPath { get; private set; }
    private readonly InstallationStateService _stateService = new();
    private List<InstallationInfo> _instances = new();
    private string? _selectedSiteName;
    private readonly bool _suppressReconfigDetection = false;

    public WelcomePage(bool suppressReconfigDetection = false)
    {
        _suppressReconfigDetection = suppressReconfigDetection;
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // For explicit fresh-install flows, do not auto-enter reconfiguration
        if (_suppressReconfigDetection)
        {
            IsReconfiguration = false;

            // Ensure any reconfiguration UI is hidden if present
            try
            {
                var pickerPanel = this.FindName("InstancePickerPanel") as FrameworkElement;
                if (pickerPanel != null)
                {
                    pickerPanel.Visibility = Visibility.Collapsed;
                }

                if (this.FindName("ExistingInstallationWarning") is FrameworkElement warn)
                {
                    warn.Visibility = Visibility.Collapsed;
                }
            }
            catch { }

            return;
        }

        DetectExistingInstallation();
    }

    private void DetectExistingInstallation()
    {
        try
        {
            // First: enumerate all instances from registry
            _instances = _stateService.GetInstallations();
            if (_instances != null && _instances.Count > 0)
            {
                // Populate picker and show panel
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
                    if (combo.Items.Count > 0)
                    {
                        combo.SelectedIndex = 0;
                    }
                }

                var pickerPanel = this.FindName("InstancePickerPanel") as FrameworkElement;
                if (pickerPanel != null)
                {
                    pickerPanel.Visibility = Visibility.Visible;
                }

                // Show existing warning for selected
                var current = _instances[0];
                _selectedSiteName = current.SiteName;
                ShowExistingInstallationWarning(current.InstallPath, current.InstallDate, current.Version);
                var summary = this.FindName("InstanceSummaryText") as TextBlock;
                if (summary != null)
                {
                    summary.Text = $"Instance: {current.SiteName} — {current.InstallPath}";
                }
                return;
            }

            // Check for mock flag first (for testing without actual install)
            var mockEnvVar = Environment.GetEnvironmentVariable("INSTALLER_MOCK_EXISTING");

            if (mockEnvVar == "true")
            {
                // Try to load saved mock state first
                var mockState = MockStateService.LoadMockState();

                if (mockState != null)
                {
                    ShowExistingInstallationWarning(mockState.InstallPath, mockState.InstallDate, mockState.Version);
                }
                else
                {
                    // Fallback to default mock state
                    var defaultState = MockStateService.GetDefaultMockState();
                    ShowExistingInstallationWarning(defaultState.InstallPath, defaultState.InstallDate, defaultState.Version);
                }

                return;
            }

            // Real detection: Check registry via InstallationStateService
            var installInfo = _stateService.GetInstallationInfo();

            if (installInfo != null)
            {
                // Verify the path still exists
                if (!string.IsNullOrEmpty(installInfo.InstallPath) && Directory.Exists(installInfo.InstallPath))
                {
                    ShowExistingInstallationWarning(installInfo.InstallPath, installInfo.InstallDate, installInfo.Version);
                    _selectedSiteName = installInfo.SiteName;
                    return;
                }
            }

            // Fallback: Check common installation paths (if registry missing)
            var commonPaths = new[]
            {
                @"C:\inetpub\EcommerceStarter",
                @"C:\inetpub\wwwroot\EcommerceStarter",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EcommerceStarter")
            };

            foreach (var checkPath in commonPaths)
            {
                if (Directory.Exists(checkPath))
                {
                    // Check if it looks like our installation
                    var webConfigPath = Path.Combine(checkPath, "web.config");
                    var appSettingsPath = Path.Combine(checkPath, "appsettings.json");

                    if (File.Exists(webConfigPath) || File.Exists(appSettingsPath))
                    {
                        ShowExistingInstallationWarning(checkPath);
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Silently fail - this is just detection, not critical
            _ = ex.Message; // Prevent unused variable warning
        }
    }

    private void ShowExistingInstallationWarning(string path, string? installDate = null, string? version = null)
    {
        ExistingInstallPath = path;
        IsReconfiguration = true; // Set reconfiguration mode automatically

        var info = "Previous installation found";

        if (!string.IsNullOrEmpty(installDate))
        {
            info = $"Installation from {installDate}";
        }

        if (!string.IsNullOrEmpty(version))
        {
            info += $" (v{version})";
        }

        ExistingInstallationInfo.Text = $"{info} at:\n{path}";
        ExistingInstallationWarning.Visibility = Visibility.Visible;

        // Update text for reconfiguration mode
        WelcomeTitle.Text = "EcommerceStarter - Reconfiguration";
        WelcomeSubtitle.Text = "Modify your existing installation or reset credentials.";
        InstallSectionTitle.Text = "What can be reconfigured:";
    }

    private void CheckPrerequisites_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Prerequisites page manually for troubleshooting
        var mainWindow = Window.GetWindow(this) as MainWindow;
        if (mainWindow != null)
        {
            // Find the Prerequisites page and navigate to it
            var pages = mainWindow.GetPages();
            var prereqPageIndex = pages.FindIndex(p => p is PrerequisitesPage);

            if (prereqPageIndex >= 0)
            {
                // Temporarily disable reconfiguration mode so Prerequisites page shows normally
                var tempReconfig = IsReconfiguration;
                IsReconfiguration = false;

                MessageBox.Show(
                    "The Prerequisites page will now open for verification.\n\n" +
                    "You can check if .NET, SQL Server, and IIS are properly installed.\n\n" +
                    "Click 'Next' when done to return to reconfiguration mode.",
                    "Check Prerequisites",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Navigate to prerequisites page (index 1)
                mainWindow.NavigateToPageByIndex(1);
            }
        }
    }

    private void InstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item && item.Tag is InstallationInfo inst)
        {
            _selectedSiteName = inst.SiteName;
            ShowExistingInstallationWarning(inst.InstallPath, inst.InstallDate, inst.Version);
            var summary = this.FindName("InstanceSummaryText") as TextBlock;
            if (summary != null)
            {
                summary.Text = $"Instance: {inst.SiteName} — {inst.InstallPath}";
            }
        }
    }

    private void RefreshInstances_Click(object sender, RoutedEventArgs e)
    {
        DetectExistingInstallation();
    }
}
