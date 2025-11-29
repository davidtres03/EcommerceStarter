using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using EcommerceStarter.Installer.Models;
using EcommerceStarter.Installer.Services;
using EcommerceStarter.Installer.Helpers;
using System.Linq;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;

namespace EcommerceStarter.Installer.Views;

public partial class ConfigurationPage : Page
{
    private readonly ConfigurationValidationService _validationService;

    public InstallationConfig Config { get; set; } = new();

    public ConfigurationPage()
    {
        InitializeComponent();
        _validationService = new ConfigurationValidationService();
        Loaded += ConfigurationPage_Loaded;

        // Auto-update installation path when company name changes
        CompanyNameTextBox.TextChanged += (s, e) => UpdateInstallationPath();

        // Port input validation: only allow numbers
        PortTextBox.PreviewTextInput += PortTextBox_PreviewTextInput;
        PortTextBox.Text = "443";

        // Set initial installation path
        UpdateInstallationPath();
    }
    // Only allow numeric input for port
    private void PortTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void ConfigurationPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Check if we're in reconfiguration mode via mock flag
        if (Environment.GetEnvironmentVariable("INSTALLER_MOCK_EXISTING") == "true")
        {
            LoadMockExistingConfiguration();
        }
    }

    private void LoadMockExistingConfiguration()
    {
        // Try to load saved mock state first
        var mockState = MockStateService.LoadMockState();

        if (mockState != null)
        {
            // Use saved state from previous installation
            System.Diagnostics.Debug.WriteLine("[ConfigurationPage] Loading SAVED mock configuration");

            CompanyNameTextBox.Text = mockState.CompanyName;
            TaglineTextBox.Text = mockState.SiteTagline;
            AdminEmailTextBox.Text = mockState.AdminEmail;
            DatabaseServerTextBox.Text = mockState.DatabaseServer;
            DatabaseNameTextBox.Text = mockState.DatabaseName;

            // Stripe configuration
            if (mockState.StripeConfigured)
            {
                ConfigureStripeCheckBox.IsChecked = true;
                if (!string.IsNullOrEmpty(mockState.StripePublishableKey))
                {
                    StripePublishableKeyTextBox.Text = mockState.StripePublishableKey;
                }
            }

            // Email configuration
            if (mockState.EmailConfigured && !string.IsNullOrEmpty(mockState.SmtpHost))
            {
                ConfigureEmailCheckBox.IsChecked = true;
                EmailProviderComboBox.SelectedIndex = 1; // SMTP
                SmtpHostTextBox.Text = mockState.SmtpHost;
                if (mockState.SmtpPort.HasValue)
                {
                    SmtpPortTextBox.Text = mockState.SmtpPort.Value.ToString();
                }
            }
        }
        else
        {
            // Fallback to default mock data
            System.Diagnostics.Debug.WriteLine("[ConfigurationPage] Loading DEFAULT mock configuration");

            var defaultState = MockStateService.GetDefaultMockState();
            CompanyNameTextBox.Text = defaultState.CompanyName;
            TaglineTextBox.Text = defaultState.SiteTagline;
            AdminEmailTextBox.Text = defaultState.AdminEmail;
            DatabaseServerTextBox.Text = defaultState.DatabaseServer;
            DatabaseNameTextBox.Text = defaultState.DatabaseName;

            if (defaultState.StripeConfigured)
            {
                ConfigureStripeCheckBox.IsChecked = true;
                if (!string.IsNullOrEmpty(defaultState.StripePublishableKey))
                {
                    StripePublishableKeyTextBox.Text = defaultState.StripePublishableKey;
                }
            }
        }

        // Note: We DON'T populate the password - user is resetting it!
        AdminPasswordBox.Password = "";
        ConfirmPasswordBox.Password = "";

        // Optionally show that Stripe was configured before
        ConfigureStripeCheckBox.IsChecked = true;
        StripePublishableKeyTextBox.Text = "pk_test_51ABC***hidden***";
        // Don't show secret key for security

        System.Diagnostics.Debug.WriteLine("[ConfigurationPage] Loaded mock existing configuration");
    }

    private void ValidateForm(object sender, TextChangedEventArgs e)
    {
        ValidateFormInternal();
    }

    private void ValidateForm(object sender, RoutedEventArgs e)
    {
        ValidateFormInternal();
    }

    private void ValidateFormInternal()
    {
        var errors = new List<string>();

        // Company Name
        if (string.IsNullOrWhiteSpace(CompanyNameTextBox.Text))
        {
            CompanyNameError.Text = "Store name is required";
            CompanyNameError.Visibility = Visibility.Visible;
            errors.Add("Store name is required");
        }
        else
        {
            CompanyNameError.Visibility = Visibility.Collapsed;
        }

        // Email validation - OPTIONAL if database exists (will skip admin creation)
        var email = AdminEmailTextBox.Text.Trim();
        var password = AdminPasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        // If either email OR password is filled, require both
        bool adminCredsProvided = !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password);

        if (adminCredsProvided)
        {
            // Email validation
            if (string.IsNullOrWhiteSpace(email))
            {
                EmailError.Text = "Email address is required if creating admin";
                EmailError.Visibility = Visibility.Visible;
                errors.Add("Email address is required");
            }
            else if (!IsValidEmail(email))
            {
                EmailError.Text = "Please enter a valid email address";
                EmailError.Visibility = Visibility.Visible;
                errors.Add("Valid email address is required");
            }
            else
            {
                EmailError.Visibility = Visibility.Collapsed;
            }

            // Password validation
            if (string.IsNullOrWhiteSpace(password))
            {
                PasswordError.Text = "Password is required";
                PasswordError.Visibility = Visibility.Visible;
                errors.Add("Password is required");
            }
            else if (password.Length < 6)
            {
                PasswordError.Text = "Password must be at least 6 characters";
                PasswordError.Visibility = Visibility.Visible;
                errors.Add("Password must be at least 6 characters");
            }
            else
            {
                PasswordError.Visibility = Visibility.Collapsed;
            }

            // Confirm password
            if (password != confirmPassword)
            {
                ConfirmPasswordError.Text = "Passwords do not match";
                ConfirmPasswordError.Visibility = Visibility.Visible;
                errors.Add("Passwords must match");
            }
            else
            {
                ConfirmPasswordError.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            // No admin creds provided - hide all errors (will skip admin creation)
            EmailError.Visibility = Visibility.Collapsed;
            PasswordError.Visibility = Visibility.Collapsed;
            ConfirmPasswordError.Visibility = Visibility.Collapsed;
        }

        // Show/hide validation summary
        if (errors.Any())
        {
            ValidationSummary.Visibility = Visibility.Visible;
            ValidationMessages.Text = string.Join("\n", errors.Select(e => $"- {e}"));
        }
        else
        {
            ValidationSummary.Visibility = Visibility.Collapsed;
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    public bool IsFormValid()
    {
        ValidateFormInternal();

        var isValid = ValidationSummary.Visibility == Visibility.Collapsed;

        var logMessage = $"[IsFormValid] Result = {isValid}, ValidationSummary.Visibility = {ValidationSummary.Visibility}";
        LogToFile(logMessage);

        return isValid;
    }

    private void LogToFile(string message)
    {
        try
        {
            var logFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "EcommerceStarter_Installer.log");
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            var fullMessage = $"{timestamp} {message}";
            System.IO.File.AppendAllText(logFile, fullMessage + Environment.NewLine);
        }
        catch { }
    }

    public InstallationConfig GetConfiguration()
    {
        Config.CompanyName = CompanyNameTextBox.Text.Trim();
        Config.SiteTagline = TaglineTextBox.Text.Trim();

        // Admin credentials are optional - only set if provided
        var email = AdminEmailTextBox.Text.Trim();
        var password = AdminPasswordBox.Password;

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            Config.AdminEmail = email;
            Config.AdminPassword = password;
        }
        else
        {
            // No admin creds - will skip admin creation (existing database scenario)
            Config.AdminEmail = string.Empty;
            Config.AdminPassword = string.Empty;
        }

        Config.DatabaseServer = DatabaseServerTextBox.Text.Trim();
        Config.DatabaseName = DatabaseNameTextBox.Text.Trim();
        Config.UseExistingDatabase = UseExistingDatabaseCheckBox.IsChecked ?? false;

        // Port
        if (int.TryParse(PortTextBox.Text.Trim(), out int portValue) && portValue > 0 && portValue < 65536)
        {
            Config.Port = portValue;
        }
        else
        {
            Config.Port = 443; // Default fallback
        }

        // HTTPS option
        Config.EnableHttps = EnableHttpsCheckBox.IsChecked ?? false;

        // Generate safe site name from company name
        var safeName = string.Join("", Config.CompanyName.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "").TrimEnd('.');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "MyStore";

        Config.SiteName = safeName;
        Config.InstallationPath = Path.Combine(@"C:\inetpub\wwwroot", safeName);

        System.Diagnostics.Debug.WriteLine($"[ConfigurationPage] GetConfiguration() called:");
        System.Diagnostics.Debug.WriteLine($"  - AdminPasswordBox.Password = '{AdminPasswordBox.Password}'");
        System.Diagnostics.Debug.WriteLine($"  - Config.AdminPassword = '{Config.AdminPassword}'");
        System.Diagnostics.Debug.WriteLine($"  - Config.InstallationPath = '{Config.InstallationPath}'");
        System.Diagnostics.Debug.WriteLine($"  - Config.SiteName = '{Config.SiteName}'");
        System.Diagnostics.Debug.WriteLine($"  - Config.Port = '{Config.Port}'");

        // Stripe configuration
        Config.ConfigureStripe = ConfigureStripeCheckBox.IsChecked ?? false;
        if (Config.ConfigureStripe)
        {
            Config.StripePublishableKey = StripePublishableKeyTextBox.Text.Trim();
            Config.StripeSecretKey = StripeSecretKeyBox.Password;
        }

        // Email configuration
        Config.ConfigureEmail = ConfigureEmailCheckBox.IsChecked ?? false;
        if (Config.ConfigureEmail)
        {
            if (EmailProviderComboBox.SelectedIndex == 0) // Resend
            {
                Config.EmailProvider = EmailProvider.Resend;
                Config.EmailApiKey = ResendApiKeyBox.Password;
            }
            else // SMTP
            {
                Config.EmailProvider = EmailProvider.Smtp;
                Config.SmtpHost = SmtpHostTextBox.Text.Trim();
                int.TryParse(SmtpPortTextBox.Text, out int port);
                Config.SmtpPort = port > 0 ? port : 587;
                Config.SmtpUsername = SmtpUsernameTextBox.Text.Trim();
                Config.SmtpPassword = SmtpPasswordBox.Password;
            }
        }

        return Config;
    }

    private void UpdateInstallationPath()
    {
        // Generate installation path based on store name
        var storeName = CompanyNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(storeName))
        {
            storeName = "MyStore";
        }

        // Remove invalid path characters and spaces
        var safeName = string.Join("", storeName.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "");

        // Set installation path
        var installPath = Path.Combine(@"C:\inetpub\wwwroot", safeName);
        InstallPathTextBox.Text = installPath;

        // Update Config object
        Config.InstallationPath = installPath;
        Config.SiteName = safeName;
    }

    #region Validation Button Handlers

    private async void TestDatabase_Click(object sender, RoutedEventArgs e)
    {
        TestDatabaseButton.IsEnabled = false;
        TestDatabaseButton.Content = "Testing...";
        DatabaseTestResult.Visibility = Visibility.Collapsed;

        try
        {
            var server = DatabaseServerTextBox.Text.Trim();
            var dbName = DatabaseNameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(dbName))
            {
                ShowTestResult(DatabaseTestResult, DatabaseTestMessage,
                    "Please enter both server and database name.", false);
                return;
            }

            var result = await _validationService.ValidateDatabaseConnectionAsync(server, dbName);

            // Just show the result - don't block the user
            ShowTestResult(DatabaseTestResult, DatabaseTestMessage, result.Message, result.IsSuccess, result.IsWarning);

            // If database exists, show helpful message but let user continue
            if (result.IsSuccess && result.Message.Contains("exists"))
            {
                DatabaseTestMessage.Text += "\n\nYou can either:\n Use this existing database (it will be upgraded)\n Change the database name above to create a new one";
            }
        }
        finally
        {
            TestDatabaseButton.IsEnabled = true;
            TestDatabaseButton.Content = "Test Connection";
        }
    }

    private async void TestStripe_Click(object sender, RoutedEventArgs e)
    {
        TestStripeButton.IsEnabled = false;
        TestStripeButton.Content = "Validating...";
        StripeTestResult.Visibility = Visibility.Collapsed;

        try
        {
            var publishableKey = StripePublishableKeyTextBox.Text.Trim();
            var secretKey = StripeSecretKeyBox.Password;

            if (string.IsNullOrWhiteSpace(publishableKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                ShowTestResult(StripeTestResult, StripeTestMessage,
                    "Please enter both Stripe keys.", false);
                return;
            }

            var result = await _validationService.ValidateStripeKeysAsync(publishableKey, secretKey);
            ShowTestResult(StripeTestResult, StripeTestMessage, result.Message, result.IsSuccess, result.IsWarning);
        }
        finally
        {
            TestStripeButton.IsEnabled = true;
            TestStripeButton.Content = "Validate Keys";
        }
    }

    private async void TestResend_Click(object sender, RoutedEventArgs e)
    {
        TestResendButton.IsEnabled = false;
        TestResendButton.Content = "Testing...";
        ResendTestResult.Visibility = Visibility.Collapsed;

        try
        {
            var apiKey = ResendApiKeyBox.Password;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                ShowTestResult(ResendTestResult, ResendTestMessage,
                    "Please enter your Resend API key.", false);
                return;
            }

            var result = await _validationService.ValidateResendApiKeyAsync(apiKey);
            ShowTestResult(ResendTestResult, ResendTestMessage, result.Message, result.IsSuccess);
        }
        finally
        {
            TestResendButton.IsEnabled = true;
            TestResendButton.Content = "Test API";
        }
    }

    private async void TestSmtp_Click(object sender, RoutedEventArgs e)
    {
        TestSmtpButton.IsEnabled = false;
        TestSmtpButton.Content = "Testing...";
        SmtpTestResult.Visibility = Visibility.Collapsed;

        try
        {
            var host = SmtpHostTextBox.Text.Trim();
            var portText = SmtpPortTextBox.Text.Trim();
            var username = SmtpUsernameTextBox.Text.Trim();
            var password = SmtpPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(host))
            {
                ShowTestResult(SmtpTestResult, SmtpTestMessage,
                    "Please enter SMTP host.", false);
                return;
            }

            if (!int.TryParse(portText, out int port))
            {
                ShowTestResult(SmtpTestResult, SmtpTestMessage,
                    "Please enter a valid port number.", false);
                return;
            }

            var result = await _validationService.ValidateSmtpConnectionAsync(host, port, username, password);
            ShowTestResult(SmtpTestResult, SmtpTestMessage, result.Message, result.IsSuccess, result.IsWarning);
        }
        finally
        {
            TestSmtpButton.IsEnabled = true;
            TestSmtpButton.Content = "Test SMTP";
        }
    }

    private void ShowTestResult(Border resultBorder, TextBlock messageBlock, string message, bool isSuccess, bool isWarning = false)
    {
        resultBorder.Visibility = Visibility.Visible;
        messageBlock.Text = message;

        if (isSuccess && !isWarning)
        {
            // Success - green
            resultBorder.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218)); // Light green
            resultBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(75, 181, 67)); // Green
            resultBorder.BorderThickness = new Thickness(1);
            messageBlock.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36)); // Dark green
        }
        else if (isSuccess && isWarning)
        {
            // Warning - yellow
            resultBorder.Background = new SolidColorBrush(Color.FromRgb(255, 243, 205)); // Light yellow
            resultBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
            resultBorder.BorderThickness = new Thickness(1);
            messageBlock.Foreground = new SolidColorBrush(Color.FromRgb(133, 100, 4)); // Dark yellow
        }
        else
        {
            // Error - red
            resultBorder.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218)); // Light red
            resultBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Red
            resultBorder.BorderThickness = new Thickness(1);
            messageBlock.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36)); // Dark red
        }
    }

    #endregion

    private void ConfigureStripe_Checked(object sender, RoutedEventArgs e)
    {
        StripeConfigPanel.Visibility = Visibility.Visible;
    }

    private void ConfigureStripe_Unchecked(object sender, RoutedEventArgs e)
    {
        StripeConfigPanel.Visibility = Visibility.Collapsed;
    }

    private void ConfigureEmail_Checked(object sender, RoutedEventArgs e)
    {
        EmailConfigPanel.Visibility = Visibility.Visible;
    }

    private void ConfigureEmail_Unchecked(object sender, RoutedEventArgs e)
    {
        EmailConfigPanel.Visibility = Visibility.Collapsed;
    }

    private void EmailProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ResendConfigPanel == null || SmtpConfigPanel == null) return;

        if (EmailProviderComboBox.SelectedIndex == 0) // Resend
        {
            ResendConfigPanel.Visibility = Visibility.Visible;
            SmtpConfigPanel.Visibility = Visibility.Collapsed;
        }
        else // SMTP
        {
            ResendConfigPanel.Visibility = Visibility.Collapsed;
            SmtpConfigPanel.Visibility = Visibility.Visible;
        }
    }

    #region Help Button Handlers

    private void StripeHelp_Click(object sender, RoutedEventArgs e)
    {
        DocumentationHelper.OpenStripeGuide();
    }

    private void EmailHelp_Click(object sender, RoutedEventArgs e)
    {
        // Show menu to choose between Resend or SMTP guide
        var selectedProvider = EmailProviderComboBox.SelectedIndex;

        if (selectedProvider == 0) // Resend
        {
            DocumentationHelper.OpenResendGuide();
        }
        else // SMTP
        {
            DocumentationHelper.OpenSmtpGuide();
        }
    }

    #endregion

    /// <summary>
    /// Load existing configuration for reconfigure mode
    /// </summary>
    public void LoadExistingConfiguration(ExistingInstallation existingInstall)
    {
        if (existingInstall == null) return;

        try
        {
            // Load basic information
            CompanyNameTextBox.Text = existingInstall.CompanyName ?? "";
            DatabaseServerTextBox.Text = existingInstall.DatabaseServer ?? "localhost\\SQLEXPRESS";
            DatabaseNameTextBox.Text = existingInstall.DatabaseName ?? "";

            // Leave admin password fields empty (user will enter new password)
            AdminEmailTextBox.Text = "";
            AdminPasswordBox.Password = "";
            ConfirmPasswordBox.Password = "";

            // Clear any validation errors since we've loaded valid data
            CompanyNameError.Visibility = Visibility.Collapsed;
            EmailError.Visibility = Visibility.Collapsed;
            PasswordError.Visibility = Visibility.Collapsed;
            ConfirmPasswordError.Visibility = Visibility.Collapsed;
            ValidationSummary.Visibility = Visibility.Collapsed;

            // Add instruction text for reconfigure mode
            var instructionText = new TextBlock
            {
                Text = "Reconfigure Mode: Enter a new admin password to reset it, or leave empty to keep existing password.",
                Foreground = System.Windows.Media.Brushes.Blue,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            // Find the admin section panel and add instruction
            var adminPanel = AdminEmailTextBox.Parent as Panel;
            if (adminPanel != null)
            {
                adminPanel.Children.Insert(0, instructionText);
            }

            // Note: Stripe and Email settings would need to be read from appsettings.json
            // For now, user can reconfigure them manually

            System.Diagnostics.Debug.WriteLine($"[ConfigurationPage] Loaded existing configuration for reconfigure mode: {existingInstall.CompanyName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConfigurationPage] Error loading existing configuration: {ex.Message}");
            MessageBox.Show(
                $"Error loading existing configuration:\n\n{ex.Message}\n\nPlease fill in the form manually.",
                "Load Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
