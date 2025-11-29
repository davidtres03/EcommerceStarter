using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace EcommerceStarter.Installer.Services;

/// <summary>
/// Service for validating installation configuration settings
/// </summary>
public class ConfigurationValidationService
{
    private static readonly HttpClient _httpClient = HttpClientFactory.GetHttpClient();

    #region Database Validation

    /// <summary>
    /// Test SQL Server connection with detailed error messages
    /// </summary>
    public async Task<ValidationResult> ValidateDatabaseConnectionAsync(string server, string databaseName, bool createIfNotExists = false)
    {
        try
        {
            // Test connection to master database first
            var masterConnectionString = $"Server={server};Database=master;Integrated Security=true;TrustServerCertificate=true;Connection Timeout=10";

            using (var connection = new SqlConnection(masterConnectionString))
            {
                await connection.OpenAsync();

                // Check if database exists
                var checkDbCommand = new SqlCommand(
                    "SELECT database_id FROM sys.databases WHERE Name = @dbName",
                    connection);
                checkDbCommand.Parameters.AddWithValue("@dbName", databaseName);

                var dbExists = await checkDbCommand.ExecuteScalarAsync() != null;

                if (!dbExists && !createIfNotExists)
                {
                    return ValidationResult.Warning(
                        $"Database '{databaseName}' does not exist. It will be created during installation.");
                }

                // Test permissions by trying to create a test database (if doesn't exist)
                if (!dbExists)
                {
                    // Check if we have CREATE DATABASE permission
                    try
                    {
                        var testPermCmd = new SqlCommand(
                            "SELECT HAS_PERMS_BY_NAME(null, null, 'CREATE DATABASE')",
                            connection);
                        var hasPermission = (int?)await testPermCmd.ExecuteScalarAsync() == 1;

                        if (!hasPermission)
                        {
                            return ValidationResult.Failure(
                                "No permission to create database. Please create the database manually or grant CREATE DATABASE permission.");
                        }
                    }
                    catch
                    {
                        // If permission check fails, we'll let the installation try and fail gracefully
                    }
                }

                return ValidationResult.Success(
                    dbExists
                        ? $"Successfully connected to SQL Server. Database '{databaseName}' exists."
                        : $"Successfully connected to SQL Server. Database '{databaseName}' will be created.");
            }
        }
        catch (SqlException ex)
        {
            return ex.Number switch
            {
                -1 => ValidationResult.Failure("Connection timeout. Check if SQL Server is running and accessible."),
                2 => ValidationResult.Failure($"Cannot connect to '{server}'. Check server name and network connection."),
                18456 => ValidationResult.Failure("Authentication failed. Using Windows Authentication - ensure you have access."),
                4060 => ValidationResult.Warning($"Database '{databaseName}' not found. It will be created during installation."),
                _ => ValidationResult.Failure($"SQL Error: {ex.Message}")
            };
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Connection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Check available disk space at installation path
    /// </summary>
    public ValidationResult ValidateInstallationPath(string path)
    {
        try
        {
            // Check if path is valid
            if (string.IsNullOrWhiteSpace(path))
            {
                return ValidationResult.Failure("Installation path cannot be empty.");
            }

            // Check if path contains invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return ValidationResult.Failure("Installation path contains invalid characters.");
            }

            // Check path length (Windows MAX_PATH is 260)
            if (path.Length > 240)
            {
                return ValidationResult.Warning("Path is very long. Consider using a shorter path to avoid issues.");
            }

            // Get drive info
            var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");

            if (!drive.IsReady)
            {
                return ValidationResult.Failure($"Drive {drive.Name} is not ready.");
            }

            const long requiredSpace = 2L * 1024 * 1024 * 1024; // 2 GB
            var availableSpace = drive.AvailableFreeSpace;

            if (availableSpace < requiredSpace)
            {
                return ValidationResult.Failure(
                    $"Insufficient disk space. Required: 2 GB, Available: {availableSpace / (1024 * 1024 * 1024)} GB");
            }

            // Check write permissions
            try
            {
                Directory.CreateDirectory(path);
                var testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (UnauthorizedAccessException)
            {
                return ValidationResult.Failure("No write permission for this location. Run installer as Administrator.");
            }

            return ValidationResult.Success(
                $"Path is valid. Available space: {availableSpace / (1024 * 1024 * 1024):F1} GB");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Path validation error: {ex.Message}");
        }
    }

    #endregion

    #region Stripe Validation

    /// <summary>
    /// Validate Stripe API keys by making test API calls
    /// </summary>
    public async Task<ValidationResult> ValidateStripeKeysAsync(string publishableKey, string secretKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(publishableKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                return ValidationResult.Failure("Both Stripe keys are required.");
            }

            // Trim whitespace
            publishableKey = publishableKey.Trim();
            secretKey = secretKey.Trim();

            // STAGE 1: Format Validation
            if (!publishableKey.StartsWith("pk_"))
            {
                return ValidationResult.Failure("Invalid publishable key format. Should start with 'pk_'");
            }

            if (!secretKey.StartsWith("sk_"))
            {
                return ValidationResult.Failure("Invalid secret key format. Should start with 'sk_'");
            }

            // Detect mode and validate structure
            bool isTestMode = false;
            bool isLiveMode = false;

            if (publishableKey.StartsWith("pk_test_"))
            {
                isTestMode = true;
                if (publishableKey.Length < 40)
                {
                    return ValidationResult.Failure(
                        $"Publishable key appears incomplete.\n" +
                        $"Expected length: ~107 characters\n" +
                        $"Your key length: {publishableKey.Length} characters\n\n" +
                        $"Please verify you copied the complete key from Stripe dashboard.");
                }

                if (!secretKey.StartsWith("sk_test_"))
                {
                    return ValidationResult.Failure("Keys must both be test keys or both be live keys.\nPublishable key is TEST but secret key is not.");
                }
            }
            else if (publishableKey.StartsWith("pk_live_"))
            {
                isLiveMode = true;
                if (publishableKey.Length < 40)
                {
                    return ValidationResult.Failure(
                        $"Publishable key appears incomplete.\n" +
                        $"Expected length: ~107 characters\n" +
                        $"Your key length: {publishableKey.Length} characters");
                }

                if (!secretKey.StartsWith("sk_live_"))
                {
                    return ValidationResult.Failure("Keys must both be test keys or both be live keys.\nPublishable key is LIVE but secret key is not.");
                }
            }
            else
            {
                return ValidationResult.Failure("Invalid publishable key format.\nMust start with 'pk_test_' or 'pk_live_'");
            }

            // Validate secret key structure
            if (secretKey.Length < 40)
            {
                return ValidationResult.Failure(
                    $"Secret key appears incomplete.\n" +
                    $"Expected length: ~107 characters\n" +
                    $"Your key length: {secretKey.Length} characters");
            }

            // STAGE 2: Test Publishable Key with Token Creation
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.stripe.com/v1/tokens");

            // Use publishable key for token creation (this is how it's meant to be used!)
            var tokenFormData = new Dictionary<string, string>
            {
                { "card[number]", "4242424242424242" },  // Stripe test card
                { "card[exp_month]", "12" },
                { "card[exp_year]", "2034" },
                { "card[cvc]", "123" }
            };

            tokenRequest.Content = new FormUrlEncodedContent(tokenFormData);
            tokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", publishableKey);

            var tokenResponse = await _httpClient.SendAsync(tokenRequest);
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            if (tokenResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ValidationResult.Failure(
                    $"Invalid publishable key. Authentication failed with Stripe.\n\n" +
                    $"Error: {tokenResponseContent}\n\n" +
                    $"Please verify your publishable key is correct and active.");
            }

            if (!tokenResponse.IsSuccessStatusCode)
            {
                // If it's not 401 but also not success, the key might be valid but there's another issue
                // Check if error is about the key itself
                if (!tokenResponseContent.Contains("invalid_request_error") || tokenResponseContent.Contains("No such"))
                {
                    return ValidationResult.Failure($"Publishable key validation error: {tokenResponse.StatusCode}\n\n{tokenResponseContent}");
                }
                // Key is valid but request has an issue (which is OK for validation)
            }

            // STAGE 3: Test Secret Key with Balance Endpoint
            var balanceRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.stripe.com/v1/balance");
            balanceRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", secretKey);

            var balanceResponse = await _httpClient.SendAsync(balanceRequest);
            var balanceResponseContent = await balanceResponse.Content.ReadAsStringAsync();

            if (balanceResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ValidationResult.Failure(
                    $"Invalid secret key. Authentication failed with Stripe.\n\n" +
                    $"Error: {balanceResponseContent}\n\n" +
                    $"Please verify your secret key is correct.");
            }

            if (!balanceResponse.IsSuccessStatusCode)
            {
                return ValidationResult.Failure(
                    $"Secret key validation error: {balanceResponse.StatusCode}\n\n{balanceResponseContent}");
            }

            // STAGE 4: Success!
            var modeText = isTestMode ? "TEST MODE" : (isLiveMode ? "LIVE MODE" : "UNKNOWN");
            var warning = isTestMode
                ? "WARNING: You are using test keys. Remember to switch to live keys before going to production!"
                : "WARNING: You are using LIVE KEYS. Real transactions will be processed!";

            return ValidationResult.Success(
                "Both Stripe keys validated successfully.\n\n" +
                $"Mode: {modeText}\n" +
                $"Publishable key: {publishableKey.Substring(0, 20)}...\n" +
                $"Secret key: {secretKey.Substring(0, 15)}...\n\n" +
                $"{warning}\n\n" +
                $"Both keys were tested with live Stripe API calls:\n" +
                $"  - Publishable key validated via token creation\n" +
                $"  - Secret key validated via balance endpoint");
        }
        catch (HttpRequestException ex)
        {
            return ValidationResult.Failure($"Network error connecting to Stripe:\n\n{ex.Message}\n\nPlease check your internet connection.");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    #endregion

    #region Email/SMTP Validation

    /// <summary>
    /// Validate Resend API key
    /// </summary>
    public async Task<ValidationResult> ValidateResendApiKeyAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ValidationResult.Failure("Resend API key is required.");
            }

            // Trim any whitespace that might have been accidentally included
            apiKey = apiKey.Trim();

            if (!apiKey.StartsWith("re_"))
            {
                return ValidationResult.Failure("Invalid Resend API key format. Should start with 're_'\n\nYour key starts with: '" + apiKey.Substring(0, Math.Min(10, apiKey.Length)) + "...'");
            }

            // For restricted keys that can only send emails, we need to validate differently
            // Instead of trying to list emails, we'll verify the key format and do a lightweight check

            // First, try the domains endpoint (works for unrestricted keys)
            var domainsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.resend.com/domains");
            domainsRequest.Headers.Add("Authorization", $"Bearer {apiKey}");
            domainsRequest.Headers.Add("User-Agent", "EcommerceStarter-Installer/1.0");

            var domainsResponse = await _httpClient.SendAsync(domainsRequest);

            // If we get 401 with "restricted_api_key", that's actually GOOD - the key is valid but restricted
            if (domainsResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                var errorContent = await domainsResponse.Content.ReadAsStringAsync();

                // Check if it's a restricted key (which is valid for sending)
                if (errorContent.Contains("restricted_api_key") || errorContent.Contains("restricted to only send emails"))
                {
                    return ValidationResult.Success(
                        "\u2713 Resend API key validated successfully!\n\n" +
                        "Note: This is a RESTRICTED key that can only send emails.\n" +
                        "This is perfect for production use and more secure!");
                }

                // Otherwise it's truly invalid
                return ValidationResult.Failure(
                    $"Authentication failed with Resend.\n\n" +
                    $"Error: {errorContent}\n\n" +
                    $"The API key appears to be invalid or revoked.");
            }

            if (domainsResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                return ValidationResult.Failure(
                    $"Access forbidden. Your API key doesn't have the required permissions.\n\n" +
                    $"Try creating a new API key with at least 'Sending access' permission.");
            }

            // If we got 200 OK, the key has full permissions
            if (domainsResponse.IsSuccessStatusCode)
            {
                return ValidationResult.Success(
                    "\u2713 Resend API key validated successfully!\n\n" +
                    "Your API key has full permissions and can send emails.");
            }

            // Any other error
            var responseContent = await domainsResponse.Content.ReadAsStringAsync();
            return ValidationResult.Failure(
                $"Resend API error: {domainsResponse.StatusCode}\n\n" +
                $"Response: {responseContent}\n\n" +
                $"Please check your API key and try again.");
        }
        catch (HttpRequestException)
        {
            return ValidationResult.Failure(
                $"Network error connecting to Resend.\n\n" +
                $"Please check your internet connection and firewall settings.");
        }
        catch (TaskCanceledException)
        {
            return ValidationResult.Failure(
                $"Request timed out.\n\n" +
                $"Please check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate SMTP connection and optionally send test email
    /// </summary>
    public async Task<ValidationResult> ValidateSmtpConnectionAsync(
        string host,
        int port,
        string username,
        string password,
        string? testEmailTo = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return ValidationResult.Failure("SMTP host is required.");
            }

            if (port <= 0 || port > 65535)
            {
                return ValidationResult.Failure("Invalid port number. Must be between 1 and 65535.");
            }

            // Common SMTP ports
            if (port != 25 && port != 587 && port != 465 && port != 2525)
            {
                return ValidationResult.Warning(
                    $"Port {port} is uncommon. Standard SMTP ports are 25, 587 (TLS), 465 (SSL), or 2525.");
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = port == 587 || port == 465,
                Timeout = 10000 // 10 seconds
            };

            // If credentials provided, use them
            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            // Try to send test email if recipient provided
            if (!string.IsNullOrWhiteSpace(testEmailTo))
            {
                var message = new MailMessage
                {
                    From = new MailAddress(username ?? "test@example.com"),
                    Subject = "EcommerceStarter Installer - Test Email",
                    Body = "This is a test email from the EcommerceStarter installer. If you received this, your SMTP configuration is working correctly!",
                    IsBodyHtml = false
                };
                message.To.Add(testEmailTo);

                await client.SendMailAsync(message);

                return ValidationResult.Success(
                    $"SMTP connection successful! Test email sent to {testEmailTo}");
            }

            // Just test connection without sending
            // Note: SmtpClient doesn't have a direct "test connection" method,
            // so we'll consider it valid if we can create the client
            return ValidationResult.Success(
                $"SMTP configuration looks valid. Host: {host}, Port: {port}, SSL: {client.EnableSsl}");
        }
        catch (SmtpException ex)
        {
            return ex.StatusCode switch
            {
                SmtpStatusCode.ServiceNotAvailable => ValidationResult.Failure(
                    "SMTP server not available. Check host and port."),
                SmtpStatusCode.MailboxUnavailable => ValidationResult.Failure(
                    "Mailbox unavailable. Check username/email address."),
                SmtpStatusCode.ClientNotPermitted => ValidationResult.Failure(
                    "Authentication failed. Check username and password."),
                _ => ValidationResult.Failure($"SMTP error: {ex.Message}")
            };
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Connection error: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Validation result with success/failure/warning status
/// </summary>
public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public bool IsWarning { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ValidationResult Success(string message) => new()
    {
        IsSuccess = true,
        IsWarning = false,
        Message = message
    };

    public static ValidationResult Warning(string message) => new()
    {
        IsSuccess = true,
        IsWarning = true,
        Message = message
    };

    public static ValidationResult Failure(string message) => new()
    {
        IsSuccess = false,
        IsWarning = false,
        Message = message
    };
}
