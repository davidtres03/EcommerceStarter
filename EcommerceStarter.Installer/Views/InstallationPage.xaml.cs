using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Models;
using EcommerceStarter.Installer.Services;

namespace EcommerceStarter.Installer.Views;

public partial class InstallationPage : Page
{
    private InstallationConfig? _config;
    private bool _installationComplete = false;
    private bool _installationStarted = false;
    private string? _errorDetails;
    private readonly InstallationStateService _stateService = new();
    private readonly LoggerService _logger = new();
    private readonly InstallationService _installationService;

    public InstallationPage()
    {
        InitializeComponent();
        
        // Create installation service with logger
        _installationService = new InstallationService(_logger);

        // Wire up InstallationService events
        _installationService.ProgressUpdate += async (s, progress) =>
        {
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateProgress(progress.CurrentStep, progress.Percentage, progress.Message, progress.StepCompleted);
            });
        };

        _installationService.StatusUpdate += (s, message) =>
        {
            Dispatcher.Invoke(() =>
            {
                StatusMessageText.Text = message;
            });
        };

        _installationService.ErrorOccurred += (s, error) =>
        {
            Dispatcher.Invoke(() =>
            {
                _errorDetails = error;
                ShowError(error, error); // Show error as both message and details
            });
        };
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Show debug mode indicator if enabled
        if (App.IsDebugMode)
        {
            Dispatcher.Invoke(() =>
            {
                SubtitleText.Text = "DEBUG MODE - No actual changes will be made to your system";
                SubtitleText.Foreground = (System.Windows.Media.Brush)FindResource("BrandWarningBrush");
            });
        }

        // Get configuration from previous page
        GetConfigurationFromWizard();

        // Populate summary
        PopulateSummary();

        // Show summary panel (not progress)
        SummaryScrollViewer.Visibility = Visibility.Visible;
        ProgressScrollViewer.Visibility = Visibility.Collapsed;
    }

    private void GetConfigurationFromWizard()
    {
        var mainWindow = Window.GetWindow(this) as MainWindow;
        if (mainWindow != null)
        {
            // Get the saved configuration from MainWindow
            _config = mainWindow.GetSavedConfiguration();

            if (_config == null)
            {
                // Fallback: Try to get from ConfigurationPage directly
                foreach (var page in mainWindow.GetPages())
                {
                    if (page is ConfigurationPage configPage)
                    {
                        _config = configPage.GetConfiguration();
                        break;
                    }
                }
            }
        }
    }

    private void PopulateSummary()
    {
        if (_config == null)
        {
            System.Diagnostics.Debug.WriteLine("[InstallationPage] ERROR: _config is NULL!");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[InstallationPage] Populating summary:");
        System.Diagnostics.Debug.WriteLine($"  - Store Name: {_config.CompanyName}");
        System.Diagnostics.Debug.WriteLine($"  - Admin Email: {_config.AdminEmail}");
        System.Diagnostics.Debug.WriteLine($"  - Admin Password: {(_config.AdminPassword?.Length > 0 ? $"[{_config.AdminPassword.Length} chars]" : "EMPTY/NULL")}");

        // Store information
        SummaryStoreName.Text = _config.CompanyName;
        SummaryAdminEmail.Text = _config.AdminEmail;
        SummaryAdminPassword.Text = _config.AdminPassword; // Show in plain text for user to save

        // Database configuration
        SummaryDbServer.Text = _config.DatabaseServer;
        SummaryDbName.Text = _config.DatabaseName;

        // Optional features
        bool hasOptional = false;

        if (_config.ConfigureStripe)
        {
            SummaryStripe.Visibility = Visibility.Visible;
            hasOptional = true;
        }

        if (_config.ConfigureEmail)
        {
            SummaryEmail.Visibility = Visibility.Visible;
            SummaryEmail.Text = $"&#x2713; Email Notifications ({_config.EmailProvider})";
            hasOptional = true;
        }

        SummaryNoOptional.Visibility = hasOptional ? Visibility.Collapsed : Visibility.Visible;
    }

    public async Task StartInstallationAsync()
    {
        if (_config == null)
        {
            ShowError("Configuration is missing. Please go back and complete the configuration.", "InstallationConfig is null");
            return;
        }

        // Hide summary, show progress
        SummaryScrollViewer.Visibility = Visibility.Collapsed;
        ProgressScrollViewer.Visibility = Visibility.Visible;

        try
        {
            if (App.IsDebugMode)
            {
                // DEBUG MODE: Simulated installation (fast, no real changes)
                await RunSimulatedInstallationAsync();
            }
            else
            {
                // PRODUCTION MODE: Real installation
                await RunRealInstallationAsync();
            }
        }
        catch (Exception ex)
        {
            ShowError($"An error occurred during installation: {ex.Message}", ex.ToString());
        }
    }

    private async Task RunSimulatedInstallationAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? DEBUG MODE: Running simulated installation");

        // Step 1: Prerequisites (20%)
        await UpdateProgress(1, 10, "Checking prerequisites...");
        await Task.Delay(2000);
        await UpdateProgress(1, 20, "Prerequisites verified!", true);

        // Step 2: Database (40%)
        await UpdateProgress(2, 25, "Creating database...");
        await Task.Delay(3000);
        await UpdateProgress(2, 40, "Database created successfully!", true);

        // Step 3: Application (60%)
        await UpdateProgress(3, 45, "Building application...");
        await Task.Delay(2000);
        await UpdateProgress(3, 55, "Deploying files...");
        await Task.Delay(2000);
        await UpdateProgress(3, 60, "Application deployed!", true);

        // Step 4: IIS Configuration (80%)
        await UpdateProgress(4, 65, "Creating IIS application pool...");
        await Task.Delay(1500);
        await UpdateProgress(4, 72, "Configuring website...");
        await Task.Delay(1500);
        await UpdateProgress(4, 80, "IIS configured successfully!", true);

        // Step 5: Configuration (90%)
        await UpdateProgress(5, 85, "Applying your settings...");
        await Task.Delay(2000);
        await UpdateProgress(5, 90, "Configuration applied!", true);

        // Step 6: Finalization (100%)
        await UpdateProgress(6, 95, "Creating shortcuts...");
        await Task.Delay(1000);

        // Save mock state for testing reconfiguration
        if (_config != null)
        {
            await UpdateProgress(6, 97, "Saving mock installation state...");
            bool saved = MockStateService.SaveMockState(_config);

            if (saved)
            {
                System.Diagnostics.Debug.WriteLine("? Mock state saved! Next run with -MockExisting will load this config.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("?? Failed to save mock state");
            }
        }

        await UpdateProgress(6, 98, "Finalizing...");
        await Task.Delay(1000);
        await UpdateProgress(6, 100, "Installation complete!", true);

        await CompleteInstallation();
    }

    private async Task RunRealInstallationAsync()
    {
        System.Diagnostics.Debug.WriteLine("?? PRODUCTION MODE: Running REAL installation");

        if (_config == null) return;

        // Use the InstallationService to perform real installation
        var result = await _installationService.InstallAsync(_config, false);

        if (result.Success)
        {
            // Save installation state to registry
            await UpdateProgress(6, 97, "Recording installation state...");
            bool saved = _stateService.SaveInstallationInfo("1.0.0", _config.InstallationPath);

            if (!saved)
            {
                System.Diagnostics.Debug.WriteLine("?? Warning: Failed to save installation state to registry");
            }

            await UpdateProgress(6, 100, "Installation complete!", true);
            await CompleteInstallation();
        }
        else
        {
            ShowError($"Installation failed: {result.ErrorMessage}", result.ErrorMessage);
        }
    }

    private async Task CompleteInstallation()
    {
        // Mark as complete
        _installationComplete = true;

        // Update UI for completion
        Dispatcher.Invoke(() =>
        {
            CurrentStepText.Text = "\u2713 Installation Complete!"; // ? checkmark
            CurrentStepText.Foreground = (System.Windows.Media.Brush)FindResource("BrandSuccessBrush");
            StatusMessageText.Text = "Your store is ready! Click Next to continue.";
            SubtitleText.Text = "Installation completed successfully!";
        });

        // Auto-navigate to completion page after 2 seconds
        await Task.Delay(2000);
        NavigateToNextPage();
    }

    private async Task UpdateProgress(int step, int percentage, string message, bool completed = false)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            // Update main progress
            MainProgressBar.Value = percentage;
            ProgressPercentageText.Text = $"{percentage}%";
            StatusMessageText.Text = message;

            // Update title based on progress
            if (percentage < 100)
            {
                CurrentStepText.Text = "Installing Your Store...";
            }

            // Update step indicator
            UpdateStepIndicator(step, completed);
        });
    }

    private void UpdateStepIndicator(int step, bool completed)
    {
        // Update the specific step
        var icon = FindName($"Step{step}Icon") as TextBlock;
        var text = FindName($"Step{step}Text") as TextBlock;

        if (icon != null && text != null)
        {
            if (completed)
            {
                // Completed step: green checkmark, full opacity
                icon.Text = "\u2713"; // ? checkmark
                icon.Foreground = (System.Windows.Media.Brush)FindResource("BrandSuccessBrush");
                icon.Opacity = 1.0;
                text.Opacity = 1.0;
            }
            else
            {
                // Current step: blue hourglass, full opacity
                icon.Text = "\u23F3"; // ? hourglass
                icon.Foreground = (System.Windows.Media.Brush)FindResource("BrandPrimaryBrush");
                icon.Opacity = 1.0;
                text.Opacity = 1.0;
            }
        }

        // Reset pending steps to faded state
        for (int i = step + 1; i <= 6; i++)
        {
            var pendingIcon = FindName($"Step{i}Icon") as TextBlock;
            var pendingText = FindName($"Step{i}Text") as TextBlock;

            if (pendingIcon != null && pendingText != null)
            {
                pendingIcon.Text = "\u23F3"; // ? hourglass
                pendingIcon.Foreground = System.Windows.Media.Brushes.Gray;
                pendingIcon.Opacity = 0.3;
                pendingText.Opacity = 0.5;
            }
        }
    }

    private void ShowError(string errorMessage, string details)
    {
        Dispatcher.Invoke(() =>
        {
            _errorDetails = details;
            ErrorMessageText.Text = errorMessage;
            ErrorPanel.Visibility = Visibility.Visible;

            CurrentStepText.Text = "\u274C Installation Failed"; // ? cross mark
            CurrentStepText.Foreground = (System.Windows.Media.Brush)FindResource("BrandDangerBrush");
            StatusMessageText.Text = "Please check the error message above.";

            MainProgressBar.Foreground = (System.Windows.Media.Brush)FindResource("BrandDangerBrush");
        });
    }

    private void ViewErrorDetails_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_errorDetails))
        {
            // Show error details in a scrollable message box for long error messages
            var detailsWindow = new Window
            {
                Title = "Error Details",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this)
            };
            
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10)
            };
            
            var textBlock = new TextBlock
            {
                Text = _errorDetails,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12
            };
            
            scrollViewer.Content = textBlock;
            detailsWindow.Content = scrollViewer;
            detailsWindow.ShowDialog();
        }
        else
        {
            MessageBox.Show("No error details available.", "Error Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void NavigateToNextPage()
    {
        // Simulate clicking the Next button
        var mainWindow = Window.GetWindow(this) as MainWindow;
        if (mainWindow != null)
        {
            var nextButton = mainWindow.FindName("NextButton") as System.Windows.Controls.Button;
            nextButton?.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }
    }

    public bool IsInstallationComplete()
    {
        return _installationComplete;
    }

    public bool CanNavigateAway()
    {
        // Don't allow navigation away during installation
        return !_installationStarted || _installationComplete;
    }

    private void CopyPassword_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_config != null && !string.IsNullOrEmpty(_config.AdminPassword))
            {
                Clipboard.SetText(_config.AdminPassword);

                // Change button text temporarily
                var originalContent = CopyPasswordButton.Content;
                CopyPasswordButton.Content = "Copied!";
                CopyPasswordButton.IsEnabled = false;

                // Reset after 2 seconds
                Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        CopyPasswordButton.Content = originalContent;
                        CopyPasswordButton.IsEnabled = true;
                    });
                });
            }
        }
        catch
        {
            MessageBox.Show("Failed to copy password to clipboard.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
