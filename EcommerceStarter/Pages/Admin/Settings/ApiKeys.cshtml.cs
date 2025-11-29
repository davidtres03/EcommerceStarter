using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Services.Tracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EcommerceStarter.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class ApiKeysModel : PageModel
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<ApiKeysModel> _logger;
        private readonly IConfiguration _configuration;

        public ApiKeysModel(
            IApiKeyService apiKeyService,
            ApplicationDbContext context,
            IEncryptionService encryption,
            ILogger<ApiKeysModel> logger,
            IConfiguration configuration)
        {
            _apiKeyService = apiKeyService;
            _context = context;
            _encryption = encryption;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public ApiKeySettingsInput Input { get; set; } = new();

        [BindProperty]
        public StripeKeysInput StripeInput { get; set; } = new();

        [BindProperty]
        public AISettingsInput AIInput { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? TestResponseJson { get; set; }

        [TempData]
        public string? ActiveTab { get; set; }

        public StripeConfiguration? CurrentStripeConfig { get; set; }
        public List<StripeConfigurationAuditLog> RecentStripeAudits { get; set; } = new();
        public string? CurrentUspsSecret { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Load carrier API settings
            var settings = await _apiKeyService.GetApiKeySettingsAsync();

            // Map to input model (don't expose encrypted passwords)
            Input = new ApiKeySettingsInput
            {
                // USPS
                UspsConsumerKey = settings.UspsUserId, // Reusing UspsUserId field for Consumer Key
                UspsEnabled = settings.UspsEnabled,
                UspsUseSandbox = settings.UspsUseSandbox,

                // UPS
                UpsClientId = settings.UpsClientId,
                UpsAccountNumber = settings.UpsAccountNumber,
                UpsEnabled = settings.UpsEnabled,

                // FedEx
                FedExAccountNumber = settings.FedExAccountNumber,
                FedExMeterNumber = settings.FedExMeterNumber,
                FedExEnabled = settings.FedExEnabled
            };

            // Track if USPS secret exists
            CurrentUspsSecret = settings.UspsPasswordEncrypted;

            // Load Stripe configuration
            CurrentStripeConfig = await _context.StripeConfigurations.FirstOrDefaultAsync();

            if (CurrentStripeConfig != null)
            {
                StripeInput.IsTestMode = CurrentStripeConfig.IsTestMode;
            }

            // Load recent Stripe audit logs
            RecentStripeAudits = await _context.StripeConfigurationAuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync();

            // Load AI configuration from database (SiteSettings)
            AIInput = new AISettingsInput
            {
                ClaudeEnabled = settings.ClaudeEnabled,
                ClaudeModel = settings.ClaudeModel ?? "claude-3-5-sonnet-20241022",
                ClaudeMaxTokens = settings.ClaudeMaxTokens,

                OllamaEnabled = settings.OllamaEnabled,
                OllamaEndpoint = settings.OllamaEndpoint ?? "http://localhost:11434",
                OllamaModel = settings.OllamaModel ?? "neural-chat",

                PreferredBackend = settings.AIPreferredBackend ?? "Auto",
                MaxCostPerRequest = settings.AIMaxCostPerRequest,
                EnableFallback = settings.AIEnableFallback
            };

            return Page();
        }

        public async Task<IActionResult> OnPostSaveStripeAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check the form for errors.";
                ActiveTab = "stripe";
                return await OnGetAsync();
            }

            try
            {
                // Validate Stripe keys format
                if (!ValidateStripeKeys())
                {
                    ActiveTab = "stripe";
                    return await OnGetAsync();
                }

                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();
                var isNewConfig = config == null;

                if (config == null)
                {
                    config = new StripeConfiguration();
                    _context.StripeConfigurations.Add(config);
                }

                // Track changes for audit
                var changes = new Dictionary<string, string>();

                if (!string.IsNullOrEmpty(StripeInput.PublishableKey))
                {
                    config.EncryptedPublishableKey = _encryption.Encrypt(StripeInput.PublishableKey);
                    changes["PublishableKey"] = "Updated";
                }

                if (!string.IsNullOrEmpty(StripeInput.SecretKey))
                {
                    config.EncryptedSecretKey = _encryption.Encrypt(StripeInput.SecretKey);
                    changes["SecretKey"] = "Updated";
                }

                if (!string.IsNullOrEmpty(StripeInput.WebhookSecret))
                {
                    config.EncryptedWebhookSecret = _encryption.Encrypt(StripeInput.WebhookSecret);
                    changes["WebhookSecret"] = "Updated";
                }

                config.IsTestMode = StripeInput.IsTestMode;
                config.LastUpdated = DateTime.UtcNow;
                config.UpdatedBy = User.Identity?.Name;

                changes["Mode"] = StripeInput.IsTestMode ? "Test Mode" : "Live Mode";

                await _context.SaveChangesAsync();

                // Log audit
                await LogStripeAuditAsync(isNewConfig ? "Created" : "Updated", JsonSerializer.Serialize(changes));

                _logger.LogWarning(
                    "Stripe API keys {Action} by {User}. Mode: {Mode}",
                    isNewConfig ? "created" : "updated",
                    User.Identity?.Name,
                    StripeInput.IsTestMode ? "Test" : "Live"
                );

                SuccessMessage = "Stripe API keys saved successfully!";
                ActiveTab = "stripe";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Stripe configuration");
                ErrorMessage = $"Error saving Stripe settings: {ex.Message}";
                ActiveTab = "stripe";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveUspsAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check the form for errors.";
                ActiveTab = "usps";
                return RedirectToPage();
            }

            var settings = await _apiKeyService.GetApiKeySettingsAsync();

            settings.UspsUserId = Input.UspsConsumerKey; // Storing Consumer Key in UspsUserId field
            settings.UspsEnabled = Input.UspsEnabled;
            settings.UspsUseSandbox = Input.UspsUseSandbox;

            // Only update secret if provided
            if (!string.IsNullOrEmpty(Input.UspsConsumerSecret))
            {
                settings.UspsPasswordEncrypted = Input.UspsConsumerSecret; // Will be encrypted by service
            }

            var success = await _apiKeyService.SaveApiKeySettingsAsync(settings, User.Identity?.Name ?? "Admin");

            if (success)
            {
                SuccessMessage = "USPS API settings saved successfully.";
                _logger.LogInformation($"USPS API settings updated by {User.Identity?.Name}");
            }
            else
            {
                ErrorMessage = "Failed to save USPS API settings.";
            }

            ActiveTab = "usps";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveUpsAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check the form for errors.";
                ActiveTab = "ups";
                return RedirectToPage();
            }

            var settings = await _apiKeyService.GetApiKeySettingsAsync();

            settings.UpsClientId = Input.UpsClientId;
            settings.UpsAccountNumber = Input.UpsAccountNumber;
            settings.UpsEnabled = Input.UpsEnabled;

            // Only update secret if provided
            if (!string.IsNullOrEmpty(Input.UpsClientSecret))
            {
                settings.UpsClientSecretEncrypted = Input.UpsClientSecret; // Will be encrypted by service
            }

            var success = await _apiKeyService.SaveApiKeySettingsAsync(settings, User.Identity?.Name ?? "Admin");

            if (success)
            {
                SuccessMessage = "UPS API settings saved successfully.";
                _logger.LogInformation($"UPS API settings updated by {User.Identity?.Name}");
            }
            else
            {
                ErrorMessage = "Failed to save UPS API settings.";
            }

            ActiveTab = "ups";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveFedExAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check the form for errors.";
                ActiveTab = "fedex";
                return RedirectToPage();
            }

            var settings = await _apiKeyService.GetApiKeySettingsAsync();

            settings.FedExAccountNumber = Input.FedExAccountNumber;
            settings.FedExMeterNumber = Input.FedExMeterNumber;
            settings.FedExEnabled = Input.FedExEnabled;

            // Only update key/password if provided
            if (!string.IsNullOrEmpty(Input.FedExKey))
            {
                settings.FedExKeyEncrypted = Input.FedExKey; // Will be encrypted by service
            }

            if (!string.IsNullOrEmpty(Input.FedExPassword))
            {
                settings.FedExPasswordEncrypted = Input.FedExPassword; // Will be encrypted by service
            }

            var success = await _apiKeyService.SaveApiKeySettingsAsync(settings, User.Identity?.Name ?? "Admin");

            if (success)
            {
                SuccessMessage = "FedEx API settings saved successfully.";
                _logger.LogInformation($"FedEx API settings updated by {User.Identity?.Name}");
            }
            else
            {
                ErrorMessage = "Failed to save FedEx API settings.";
            }

            ActiveTab = "fedex";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveAIAsync()
        {
            try
            {
                _logger.LogInformation("OnPostSaveAIAsync called");
                _logger.LogInformation("AIInput.ClaudeEnabled: {ClaudeEnabled}", AIInput.ClaudeEnabled);
                _logger.LogInformation("AIInput.OllamaEnabled: {OllamaEnabled}", AIInput.OllamaEnabled);
                _logger.LogInformation("AIInput.PreferredBackend: {PreferredBackend}", AIInput.PreferredBackend);
                _logger.LogInformation("AIInput.MaxCostPerRequest: {MaxCostPerRequest}", AIInput.MaxCostPerRequest);
                _logger.LogInformation("AIInput.EnableFallback: {EnableFallback}", AIInput.EnableFallback);

                var settings = await _apiKeyService.GetApiKeySettingsAsync();

                // Update AI settings
                settings.ClaudeEnabled = AIInput.ClaudeEnabled;
                settings.ClaudeModel = AIInput.ClaudeModel ?? "claude-3-5-sonnet-20241022";
                settings.ClaudeMaxTokens = AIInput.ClaudeMaxTokens;

                // Only update API key if provided
                if (!string.IsNullOrEmpty(AIInput.ClaudeApiKey))
                {
                    settings.ClaudeApiKeyEncrypted = AIInput.ClaudeApiKey; // Will be encrypted by service
                }

                settings.OllamaEnabled = AIInput.OllamaEnabled;
                settings.OllamaEndpoint = AIInput.OllamaEndpoint ?? "http://localhost:11434";
                settings.OllamaModel = AIInput.OllamaModel ?? "neural-chat";

                settings.AIPreferredBackend = AIInput.PreferredBackend ?? "Auto";
                settings.AIMaxCostPerRequest = AIInput.MaxCostPerRequest;
                settings.AIEnableFallback = AIInput.EnableFallback;

                settings.LastUpdated = DateTime.UtcNow;
                settings.LastUpdatedBy = User.Identity?.Name;

                _logger.LogInformation("Calling SaveApiKeySettingsAsync");
                await _apiKeyService.SaveApiKeySettingsAsync(settings, User.Identity?.Name ?? "System");

                _logger.LogInformation("AI settings updated by {User}", User.Identity?.Name);

                SuccessMessage = "AI settings saved successfully! Claude API key is encrypted in the database.";
                ActiveTab = "ai";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving AI settings");
                ErrorMessage = "Failed to save AI settings: " + ex.Message;
                ActiveTab = "ai";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostTestUspsAsync()
        {
            // Set active tab to USPS - do this first, don't clear on error
            ActiveTab = "usps";

            try
            {
                // Get credentials and settings
                var settings = await _apiKeyService.GetApiKeySettingsAsync();
                var consumerKey = await _apiKeyService.GetUspsUserIdAsync();
                var consumerSecret = await _apiKeyService.GetUspsPasswordAsync();

                if (string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret))
                {
                    ErrorMessage = "USPS Consumer Key or Secret not configured. Please save your settings first.";

                    TestResponseJson = JsonSerializer.Serialize(new
                    {
                        success = false,
                        timestamp = DateTime.UtcNow.ToString("O"),
                        environment = settings.UspsUseSandbox ? "SANDBOX" : "PRODUCTION",
                        error = "Missing credentials"
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
                else
                {
                    consumerKey = consumerKey.Trim();
                    consumerSecret = consumerSecret.Trim();

                    // Determine which endpoint to use
                    var baseUrl = settings.UspsUseSandbox ? "https://apis-tem.usps.com" : "https://apis.usps.com";
                    var authUrl = $"{baseUrl}/oauth2/v3/token";
                    var zipCodeUrl = $"{baseUrl}/addresses/v3/zipcode?streetAddress=520%20Maryville%20Centre%20Drive&city=Saint%20Louis&state=MO";

                    _logger.LogInformation($"Testing USPS connection with {(settings.UspsUseSandbox ? "SANDBOX" : "PRODUCTION")} environment");

                    // Step 1: Get OAuth Token
                    var oauthResponse = await GetUspsOAuthTokenAsync(authUrl, consumerKey, consumerSecret);

                    if (oauthResponse == null || string.IsNullOrEmpty(oauthResponse.AccessToken))
                    {
                        ErrorMessage = "Failed to obtain OAuth token. Check your credentials.";

                        TestResponseJson = JsonSerializer.Serialize(new
                        {
                            success = false,
                            timestamp = DateTime.UtcNow.ToString("O"),
                            environment = settings.UspsUseSandbox ? "SANDBOX" : "PRODUCTION",
                            steps = new
                            {
                                oauth = new { status = "FAILED", message = "Failed to obtain token" },
                                zipcode = new { status = "SKIPPED", message = "Token required for next step" }
                            },
                            error = "OAuth token request failed"
                        }, new JsonSerializerOptions { WriteIndented = true });

                        _logger.LogWarning("USPS OAuth token request failed");
                    }
                    else
                    {
                        var accessToken = oauthResponse.AccessToken;

                        // Step 2: Call ZipCode API
                        var zipCodeResponse = await GetUspsZipCodeAsync(zipCodeUrl, accessToken);

                        // Build comprehensive response
                        var responseData = new
                        {
                            success = zipCodeResponse.HasValue,
                            timestamp = DateTime.UtcNow.ToString("O"),
                            environment = settings.UspsUseSandbox ? "SANDBOX" : "PRODUCTION",
                            steps = new
                            {
                                oauth = new
                                {
                                    status = "SUCCESS",
                                    message = "OAuth token obtained",
                                    details = new
                                    {
                                        tokenType = oauthResponse.TokenType,
                                        expiresIn = $"{oauthResponse.ExpiresIn} seconds (~{Math.Round(oauthResponse.ExpiresIn / 3600d)} hours)",
                                        tokenLength = oauthResponse.AccessToken.Length
                                    }
                                },
                                zipcode = new
                                {
                                    status = zipCodeResponse.HasValue ? "SUCCESS" : "FAILED",
                                    message = zipCodeResponse.HasValue ? "ZipCode lookup successful" : "ZipCode lookup failed",
                                    details = zipCodeResponse
                                }
                            }
                        };

                        TestResponseJson = JsonSerializer.Serialize(responseData, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                        if (zipCodeResponse.HasValue)
                        {
                            try
                            {
                                var addr = zipCodeResponse.Value;
                                var street = addr.TryGetProperty("address", out var addressObj) &&
                                           addressObj.TryGetProperty("streetAddress", out var streetProp)
                                    ? streetProp.GetString() : "Unknown";
                                var city = addr.TryGetProperty("address", out var addressObj2) &&
                                          addressObj2.TryGetProperty("city", out var cityProp)
                                    ? cityProp.GetString() : "Unknown";
                                var state = addr.TryGetProperty("address", out var addressObj3) &&
                                           addressObj3.TryGetProperty("state", out var stateProp)
                                    ? stateProp.GetString() : "Unknown";
                                var zip = addr.TryGetProperty("address", out var addressObj4) &&
                                         addressObj4.TryGetProperty("ZIPCode", out var zipProp)
                                    ? zipProp.GetString() : "Unknown";

                                SuccessMessage = $"[SUCCESS] USPS API test successful! OAuth token obtained and ZipCode API working. " +
                                               $"Address: {street}, {city}, {state} {zip}";
                            }
                            catch
                            {
                                SuccessMessage = $"[SUCCESS] USPS API test successful! OAuth token obtained and ZipCode API working.";
                            }

                            _logger.LogInformation($"USPS test successful: OAuth and ZipCode API working");
                        }
                        else
                        {
                            ErrorMessage = $"[WARNING] OAuth token obtained but ZipCode API failed. Check logs for details.";
                            _logger.LogWarning("USPS ZipCode API call failed");
                        }
                    }
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing USPS connection");
                ErrorMessage = $"[ERROR] USPS test failed: {ex.Message}";

                TestResponseJson = JsonSerializer.Serialize(new
                {
                    success = false,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    error = ex.Message,
                    message = "Exception occurred during test"
                }, new JsonSerializerOptions { WriteIndented = true });

                return RedirectToPage();
            }
        }

        private async Task<OAuthTokenResponse?> GetUspsOAuthTokenAsync(string authUrl, string consumerKey, string consumerSecret)
        {
            try
            {
                var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                var oauthBody = new
                {
                    client_id = consumerKey,
                    client_secret = consumerSecret,
                    grant_type = "client_credentials"
                };

                var jsonBody = JsonSerializer.Serialize(oauthBody);
                var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, authUrl) { Content = content };
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OAuth failed: {response.StatusCode} - {responseContent}");
                    return null;
                }

                var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(responseContent);
                return tokenResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OAuth token");
                return null;
            }
        }

        private async Task<JsonElement?> GetUspsZipCodeAsync(string zipCodeUrl, string accessToken)
        {
            try
            {
                var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                var request = new HttpRequestMessage(HttpMethod.Get, zipCodeUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"ZipCode API failed: {response.StatusCode} - {responseContent}");
                    return null;
                }

                var doc = JsonDocument.Parse(responseContent);
                return doc.RootElement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ZipCode API");
                return null;
            }
        }

        // OAuth response model
        private class OAuthTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }
        }

        private bool ValidateStripeKeys()
        {
            var hasErrors = false;

            if (!string.IsNullOrEmpty(StripeInput.PublishableKey))
            {
                var expectedPrefix = StripeInput.IsTestMode ? "pk_test_" : "pk_live_";
                if (!StripeInput.PublishableKey.StartsWith(expectedPrefix))
                {
                    ModelState.AddModelError($"{nameof(StripeInput)}.{nameof(StripeInput.PublishableKey)}",
                        $"Publishable key must start with '{expectedPrefix}' for {(StripeInput.IsTestMode ? "test" : "live")} mode");
                    hasErrors = true;
                }
            }

            if (!string.IsNullOrEmpty(StripeInput.SecretKey))
            {
                var expectedPrefix = StripeInput.IsTestMode ? "sk_test_" : "sk_live_";
                if (!StripeInput.SecretKey.StartsWith(expectedPrefix))
                {
                    ModelState.AddModelError($"{nameof(StripeInput)}.{nameof(StripeInput.SecretKey)}",
                        $"Secret key must start with '{expectedPrefix}' for {(StripeInput.IsTestMode ? "test" : "live")} mode");
                    hasErrors = true;
                }
            }

            if (!string.IsNullOrEmpty(StripeInput.WebhookSecret))
            {
                if (!StripeInput.WebhookSecret.StartsWith("whsec_"))
                {
                    ModelState.AddModelError($"{nameof(StripeInput)}.{nameof(StripeInput.WebhookSecret)}",
                        "Webhook secret must start with 'whsec_'");
                    hasErrors = true;
                }
            }

            return !hasErrors;
        }

        private async Task LogStripeAuditAsync(string action, string? changes)
        {
            try
            {
                var audit = new StripeConfigurationAuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    UserEmail = User.Identity?.Name,
                    Action = action,
                    Changes = changes,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    WasTestMode = StripeInput.IsTestMode
                };

                _context.StripeConfigurationAuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging Stripe audit");
            }
        }

        public class ApiKeySettingsInput
        {
            // USPS (Consumer Key + Consumer Secret for OAuth)
            public string? UspsConsumerKey { get; set; }
            public string? UspsConsumerSecret { get; set; } // Plain text input, will be encrypted
            public bool UspsEnabled { get; set; }
            public bool UspsUseSandbox { get; set; } // Toggle between sandbox and production

            // UPS
            public string? UpsClientId { get; set; }
            public string? UpsClientSecret { get; set; } // Plain text input, will be encrypted
            public string? UpsAccountNumber { get; set; }
            public bool UpsEnabled { get; set; }

            // FedEx
            public string? FedExAccountNumber { get; set; }
            public string? FedExMeterNumber { get; set; }
            public string? FedExKey { get; set; } // Plain text input, will be encrypted
            public string? FedExPassword { get; set; } // Plain text input, will be encrypted
            public bool FedExEnabled { get; set; }
        }

        public class StripeKeysInput
        {
            public string? PublishableKey { get; set; }
            public string? SecretKey { get; set; }
            public string? WebhookSecret { get; set; }
            public bool IsTestMode { get; set; } = true;
        }

        public class AISettingsInput
        {
            // Claude
            public bool ClaudeEnabled { get; set; }
            public string? ClaudeApiKey { get; set; }
            public string? ClaudeModel { get; set; } = "claude-3-5-sonnet-20241022";
            public int ClaudeMaxTokens { get; set; } = 2000;

            // Ollama
            public bool OllamaEnabled { get; set; } = true;
            public string? OllamaEndpoint { get; set; } = "http://localhost:11434";
            public string? OllamaModel { get; set; } = "neural-chat";

            // Routing
            public string? PreferredBackend { get; set; } = "Auto";
            public decimal MaxCostPerRequest { get; set; } = 0.10m;
            public bool EnableFallback { get; set; } = true;
        }
    }
}
