using System.Security.Claims;
using System.Text.Json;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using EcommerceStarter.Services;
using EcommerceStarter.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Pages.Admin.Settings
{
    [Authorize(Roles = "Admin")]
    public class ApiConfigurationsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IApiConfigurationService _apiConfigService;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<ApiConfigurationsModel> _logger;

        public ApiConfigurationsModel(
            ApplicationDbContext context,
            IApiConfigurationService apiConfigService,
            ISiteSettingsService siteSettingsService,
            ILogger<ApiConfigurationsModel> logger)
        {
            _context = context;
            _apiConfigService = apiConfigService;
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        // Input models for form binding
        [BindProperty]
        public StripeConfigInput StripeInput { get; set; } = new();

        [BindProperty]
        public CloudinaryConfigInput CloudinaryInput { get; set; } = new();

        [BindProperty]
        public ShippingApiConfigInput UspsInput { get; set; } = new();

        [BindProperty]
        public ShippingApiConfigInput UpsInput { get; set; } = new();

        [BindProperty]
        public ShippingApiConfigInput FedexInput { get; set; } = new();

        [BindProperty]
        public AIConfigInput ClaudeInput { get; set; } = new();

        [BindProperty]
        public AIConfigInput OllamaInput { get; set; } = new();

        [BindProperty]
        public EmailConfigInput EmailInput { get; set; } = new();

        // Display models
        public List<ApiConfiguration> StripeConfigurations { get; set; } = new();
        public List<ApiConfiguration> CloudinaryConfigurations { get; set; } = new();
        public List<ApiConfiguration> ShippingConfigurations { get; set; } = new();
        public List<ApiConfiguration> AIConfigurations { get; set; } = new();
        public List<ApiConfiguration> EmailConfigurations { get; set; } = new();

        public List<ApiConfigurationAuditLog> RecentAudits { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? ActiveTab { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load all configurations by type
                StripeConfigurations = await _apiConfigService.GetConfigurationsByTypeAsync("Stripe", false);
                CloudinaryConfigurations = await _apiConfigService.GetConfigurationsByTypeAsync("Cloudinary", false);
                ShippingConfigurations = await _context.ApiConfigurations
                    .Where(ac => ac.ApiType == "USPS" || ac.ApiType == "UPS" || ac.ApiType == "FedEx")
                    .OrderBy(ac => ac.ApiType)
                    .ToListAsync();
                AIConfigurations = await _context.ApiConfigurations
                    .Where(ac => ac.ApiType == "Claude" || ac.ApiType == "Ollama")
                    .OrderBy(ac => ac.ApiType)
                    .ToListAsync();
                EmailConfigurations = await _apiConfigService.GetConfigurationsByTypeAsync("Email", false);

                // Pre-populate form fields with existing configurations (decrypted values)
                // This allows admins to edit existing configurations
                await PrePopulateStripeFormAsync();
                await PrePopulateCloudinaryFormAsync();
                await PrePopulateShippingFormAsync();
                await PrePopulateAIFormAsync();
                await PrePopulateEmailFormAsync();

                // Load recent audits
                RecentAudits = await _context.ApiConfigurationAuditLogs
                    .Include(a => a.ApiConfiguration)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(20)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading API configurations");
                ErrorMessage = "Error loading configurations";
                return Page();
            }
        }

        private async Task PrePopulateEmailFormAsync()
        {
            // Load existing Email config if present
            var emailConfig = await _context.ApiConfigurations
                .Where(c => c.ApiType == "Email")
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (emailConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(emailConfig.Id);
                var metadata = !string.IsNullOrEmpty(emailConfig.MetadataJson)
                    ? JsonSerializer.Deserialize<JsonElement>(emailConfig.MetadataJson)
                    : (JsonElement?)null;

                string provider = metadata?.GetProperty("provider").GetString() ?? "None";
                string from = metadata?.GetProperty("from").GetString() ?? string.Empty;

                EmailInput = new EmailConfigInput
                {
                    Provider = provider,
                    FromAddress = from,
                    ResendApiKey = decrypted.GetValueOrDefault("Value1"),
                    ResendDomain = decrypted.GetValueOrDefault("Value2"),
                    SmtpHost = decrypted.GetValueOrDefault("Value3"),
                    SmtpPort = int.TryParse(decrypted.GetValueOrDefault("Value4"), out var p) ? p : 587,
                    SmtpUsername = decrypted.GetValueOrDefault("Value5"),
                    SmtpPassword = decrypted.GetValueOrDefault("Value6"),
                    SmtpUseSsl = (metadata?.TryGetProperty("smtpUseSsl", out var sslProp) == true) && sslProp.GetBoolean()
                };
            }
        }

        private async Task PrePopulateStripeFormAsync()
        {
            // Check if Stripe config exists and pre-populate form
            var stripeConfig = StripeConfigurations.FirstOrDefault();
            if (stripeConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(stripeConfig.Id);
                StripeInput = new StripeConfigInput
                {
                    PublishableKey = decrypted?["Value1"],
                    SecretKey = decrypted?["Value2"],
                    WebhookSecret = decrypted?["Value3"],
                    IsTestMode = stripeConfig.IsTestMode,
                    Description = stripeConfig.Description
                };
            }
        }

        private async Task PrePopulateCloudinaryFormAsync()
        {
            var cloudinaryConfig = CloudinaryConfigurations.FirstOrDefault();
            if (cloudinaryConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(cloudinaryConfig.Id);
                CloudinaryInput = new CloudinaryConfigInput
                {
                    CloudName = decrypted?["Value1"],
                    ApiKey = decrypted?["Value2"],
                    ApiSecret = decrypted?["Value3"],
                    Description = cloudinaryConfig.Description
                };
            }
        }

        private async Task PrePopulateShippingFormAsync()
        {
            var uspsConfig = ShippingConfigurations.FirstOrDefault(c => c.ApiType == "USPS");
            if (uspsConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(uspsConfig.Id);
                UspsInput = new ShippingApiConfigInput
                {
                    Key1 = decrypted?["Value1"],
                    Key2 = decrypted?["Value2"],
                    Key3 = decrypted?["Value3"],
                    Key4 = decrypted?["Value4"],
                    UseSandbox = uspsConfig.IsTestMode,
                    Description = uspsConfig.Description
                };
            }

            var upsConfig = ShippingConfigurations.FirstOrDefault(c => c.ApiType == "UPS");
            if (upsConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(upsConfig.Id);
                UpsInput = new ShippingApiConfigInput
                {
                    Key1 = decrypted?["Value1"],
                    Key2 = decrypted?["Value2"],
                    Key3 = decrypted?["Value3"],
                    Key4 = decrypted?["Value4"],
                    UseSandbox = upsConfig.IsTestMode,
                    Description = upsConfig.Description
                };
            }

            var fedexConfig = ShippingConfigurations.FirstOrDefault(c => c.ApiType == "FedEx");
            if (fedexConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(fedexConfig.Id);
                FedexInput = new ShippingApiConfigInput
                {
                    Key1 = decrypted?["Value1"],
                    Key2 = decrypted?["Value2"],
                    Key3 = decrypted?["Value3"],
                    Key4 = decrypted?["Value4"],
                    UseSandbox = fedexConfig.IsTestMode,
                    Description = fedexConfig.Description
                };
            }
        }

        private async Task PrePopulateAIFormAsync()
        {
            var claudeConfig = AIConfigurations.FirstOrDefault(c => c.ApiType == "Claude");
            if (claudeConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(claudeConfig.Id);
                var metadata = !string.IsNullOrEmpty(claudeConfig.MetadataJson)
                    ? JsonSerializer.Deserialize<JsonElement>(claudeConfig.MetadataJson)
                    : (JsonElement?)null;

                _logger.LogInformation("[PrePopulate] Claude MetadataJson: {Json}", claudeConfig.MetadataJson);

                string? model = "claude-3-sonnet-20240229";
                int maxTokens = 2000;
                bool enabled = false;

                if (metadata?.TryGetProperty("model", out var modelProp) == true)
                    model = modelProp.GetString();
                if (metadata?.TryGetProperty("Model", out var modelProp2) == true)
                    model = modelProp2.GetString();
                if (metadata?.TryGetProperty("maxTokens", out var tokensProp) == true)
                    maxTokens = tokensProp.GetInt32();
                if (metadata?.TryGetProperty("MaxTokens", out var tokensProp2) == true)
                    maxTokens = tokensProp2.GetInt32();
                if (metadata?.TryGetProperty("enabled", out var enabledProp) == true)
                    enabled = enabledProp.GetBoolean();
                if (metadata?.TryGetProperty("Enabled", out var enabledProp2) == true)
                    enabled = enabledProp2.GetBoolean();

                _logger.LogInformation("[PrePopulate] Claude - Model: {Model}, Enabled: {Enabled}", model, enabled);

                ClaudeInput = new AIConfigInput
                {
                    ApiKey = decrypted?["Value1"],
                    SecondaryKey = decrypted?["Value2"],
                    Model = model,
                    MaxTokens = maxTokens,
                    Enabled = enabled,
                    Description = claudeConfig.Description
                };
            }

            var ollamaConfig = AIConfigurations.FirstOrDefault(c => c.ApiType == "Ollama");
            if (ollamaConfig != null)
            {
                var decrypted = await _apiConfigService.GetDecryptedValuesAsync(ollamaConfig.Id);
                var metadata = !string.IsNullOrEmpty(ollamaConfig.MetadataJson)
                    ? JsonSerializer.Deserialize<JsonElement>(ollamaConfig.MetadataJson)
                    : (JsonElement?)null;

                _logger.LogInformation("[PrePopulate] Ollama MetadataJson: {Json}", ollamaConfig.MetadataJson);

                string? endpoint = "http://localhost:11434";
                string? model = "llama2";
                int maxTokens = 2000;
                bool enabled = false;

                if (metadata?.TryGetProperty("endpoint", out var endpointProp) == true)
                    endpoint = endpointProp.GetString();
                if (metadata?.TryGetProperty("Endpoint", out var endpointProp2) == true)
                    endpoint = endpointProp2.GetString();
                if (metadata?.TryGetProperty("model", out var modelProp) == true)
                    model = modelProp.GetString();
                if (metadata?.TryGetProperty("Model", out var modelProp2) == true)
                    model = modelProp2.GetString();
                if (metadata?.TryGetProperty("maxTokens", out var tokensProp) == true)
                    maxTokens = tokensProp.GetInt32();
                if (metadata?.TryGetProperty("MaxTokens", out var tokensProp2) == true)
                    maxTokens = tokensProp2.GetInt32();
                if (metadata?.TryGetProperty("enabled", out var enabledProp) == true)
                    enabled = enabledProp.GetBoolean();
                if (metadata?.TryGetProperty("Enabled", out var enabledProp2) == true)
                    enabled = enabledProp2.GetBoolean();

                _logger.LogInformation("[PrePopulate] Ollama - Model: {Model}, Endpoint: {Endpoint}, Enabled: {Enabled}", model, endpoint, enabled);

                OllamaInput = new AIConfigInput
                {
                    ApiKey = decrypted?["Value1"],
                    SecondaryKey = decrypted?["Value2"],
                    Endpoint = endpoint,
                    Model = model,
                    MaxTokens = maxTokens,
                    Enabled = enabled,
                    Description = ollamaConfig.Description
                };
            }
        }

        // Stripe configuration management
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
                // Validate Stripe keys
                if (!string.IsNullOrEmpty(StripeInput.PublishableKey))
                {
                    var prefix = StripeInput.IsTestMode ? "pk_test_" : "pk_live_";
                    if (!StripeInput.PublishableKey.StartsWith(prefix))
                    {
                        ModelState.AddModelError(nameof(StripeInput.PublishableKey),
                            $"Publishable key must start with '{prefix}'");
                    }
                }

                if (!string.IsNullOrEmpty(StripeInput.SecretKey))
                {
                    var prefix = StripeInput.IsTestMode ? "sk_test_" : "sk_live_";
                    if (!StripeInput.SecretKey.StartsWith(prefix))
                    {
                        ModelState.AddModelError(nameof(StripeInput.SecretKey),
                            $"Secret key must start with '{prefix}'");
                    }
                }

                if (!ModelState.IsValid)
                {
                    ActiveTab = "stripe";
                    return await OnGetAsync();
                }

                var name = StripeInput.IsTestMode ? "Stripe-Test" : "Stripe-Live";
                var values = new Dictionary<string, string?>
                {
                    { "Value1", StripeInput.PublishableKey },
                    { "Value2", StripeInput.SecretKey },
                    { "Value3", StripeInput.WebhookSecret }
                };

                await _apiConfigService.SaveConfigurationAsync(
                    "Stripe",
                    name,
                    values,
                    description: StripeInput.Description,
                    isTestMode: StripeInput.IsTestMode,
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    userEmail: User.Identity?.Name,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                SuccessMessage = "Stripe configuration saved successfully!";
                ActiveTab = "stripe";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Stripe configuration");
                ErrorMessage = $"Error saving Stripe configuration: {ex.Message}";
                ActiveTab = "stripe";
            }

            return RedirectToPage();
        }

        // Cloudinary configuration management
        public async Task<IActionResult> OnPostSaveCloudinaryAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please check the form for errors.";
                ActiveTab = "cloudinary";
                return await OnGetAsync();
            }

            try
            {
                var values = new Dictionary<string, string?>
                {
                    { "Value1", CloudinaryInput.CloudName },
                    { "Value2", CloudinaryInput.ApiKey },
                    { "Value3", CloudinaryInput.ApiSecret }
                };

                await _apiConfigService.SaveConfigurationAsync(
                    "Cloudinary",
                    "Cloudinary-Main",
                    values,
                    metadata: JsonSerializer.Serialize(new { }),
                    description: CloudinaryInput.Description ?? "Primary Cloudinary configuration",
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    userEmail: User.Identity?.Name,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                SuccessMessage = "Cloudinary configuration saved successfully!";
                ActiveTab = "cloudinary";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Cloudinary configuration");
                ErrorMessage = $"Error saving Cloudinary configuration: {ex.Message}";
                ActiveTab = "cloudinary";
            }

            return RedirectToPage();
        }

        // Generic shipping API handler (USPS/UPS/FedEx)
        public async Task<IActionResult> OnPostSaveShippingAsync(string apiType)
        {
            try
            {
                ShippingApiConfigInput input;
                string name;

                if (apiType == "USPS")
                {
                    input = UspsInput;
                    name = "USPS-Production";
                }
                else if (apiType == "UPS")
                {
                    input = UpsInput;
                    name = "UPS-Production";
                }
                else if (apiType == "FedEx")
                {
                    input = FedexInput;
                    name = "FedEx-Production";
                }
                else
                {
                    ErrorMessage = $"Unknown API type: {apiType}";
                    ActiveTab = apiType.ToLower();
                    return await OnGetAsync();
                }

                var values = new Dictionary<string, string?>
                {
                    { "Value1", input.Key1 },
                    { "Value2", input.Key2 },
                    { "Value3", input.Key3 },
                    { "Value4", input.Key4 }
                };

                await _apiConfigService.SaveConfigurationAsync(
                    apiType,
                    name,
                    values,
                    metadata: JsonSerializer.Serialize(new { useSandbox = input.UseSandbox }),
                    description: input.Description,
                    isTestMode: input.UseSandbox,
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    userEmail: User.Identity?.Name,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                SuccessMessage = $"{apiType} configuration saved successfully!";
                ActiveTab = apiType.ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {ApiType} configuration", apiType);
                ErrorMessage = $"Error saving {apiType} configuration: {ex.Message}";
                ActiveTab = apiType.ToLower();
            }

            return RedirectToPage();
        }

        // AI configuration management (Claude/Ollama)
        public async Task<IActionResult> OnPostSaveAIAsync(string aiType)
        {
            try
            {
                AIConfigInput input;
                string name;

                if (aiType == "Claude")
                {
                    input = ClaudeInput;
                    name = "Claude-Production";
                    _logger.LogInformation("[SaveAI] Claude - Enabled: {Enabled}, Model: {Model}, Endpoint: {Endpoint}", 
                        input.Enabled, input.Model, input.Endpoint);
                }
                else if (aiType == "Ollama")
                {
                    input = OllamaInput;
                    name = "Ollama-Local";
                    _logger.LogInformation("[SaveAI] Ollama - Enabled: {Enabled}, Model: {Model}, Endpoint: {Endpoint}", 
                        input.Enabled, input.Model, input.Endpoint);
                }
                else
                {
                    ErrorMessage = $"Unknown AI type: {aiType}";
                    ActiveTab = aiType.ToLower();
                    return await OnGetAsync();
                }

                var metadata = JsonSerializer.Serialize(new
                {
                    input.Endpoint,
                    input.Model,
                    input.MaxTokens,
                    input.Enabled
                });

                _logger.LogInformation("[SaveAI] Metadata JSON: {Metadata}", metadata);

                var values = new Dictionary<string, string?>
                {
                    { "Value1", input.ApiKey },
                    { "Value2", input.SecondaryKey }
                };

                await _apiConfigService.SaveConfigurationAsync(
                    aiType,
                    name,
                    values,
                    metadata: metadata,
                    description: input.Description,
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    userEmail: User.Identity?.Name,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // IMPORTANT: Invalidate AI service cache to force immediate reload
                try
                {
                    var ollamaService = HttpContext.RequestServices.GetRequiredService<OllamaService>();
                    var claudeService = HttpContext.RequestServices.GetRequiredService<ClaudeAIService>();
                    
                    ollamaService.InvalidateCache();
                    claudeService.InvalidateCache();
                    
                    _logger.LogInformation("AI service caches invalidated - changes will take effect immediately");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not invalidate AI service caches");
                }

                SuccessMessage = $"{aiType} configuration saved successfully! (Enabled: {input.Enabled}, Model: {input.Model})";
                ActiveTab = aiType.ToLower();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {AiType} configuration", aiType);
                ErrorMessage = $"Error saving {aiType} configuration: {ex.Message}";
                ActiveTab = aiType.ToLower();
            }

            return RedirectToPage();
        }

        // Email configuration management
        public async Task<IActionResult> OnPostSaveEmailAsync()
        {
            try
            {
                // Basic validation
                if (string.IsNullOrWhiteSpace(EmailInput.Provider))
                {
                    ModelState.AddModelError(nameof(EmailInput.Provider), "Provider is required");
                }

                if (!ModelState.IsValid)
                {
                    ActiveTab = "email";
                    return await OnGetAsync();
                }

                var values = new Dictionary<string, string?>();

                // Map values depending on provider
                if (EmailInput.Provider == "Resend")
                {
                    values["Value1"] = EmailInput.ResendApiKey;
                    values["Value2"] = EmailInput.ResendDomain;
                }
                else if (EmailInput.Provider == "SMTP")
                {
                    values["Value3"] = EmailInput.SmtpHost;
                    values["Value4"] = EmailInput.SmtpPort.ToString();
                    values["Value5"] = EmailInput.SmtpUsername;
                    values["Value6"] = EmailInput.SmtpPassword;
                }

                var metadata = JsonSerializer.Serialize(new
                {
                    provider = EmailInput.Provider,
                    from = EmailInput.FromAddress,
                    smtpUseSsl = EmailInput.SmtpUseSsl
                });

                await _apiConfigService.SaveConfigurationAsync(
                    "Email",
                    $"Email-{EmailInput.Provider}",
                    values,
                    metadata: metadata,
                    description: "Transactional email settings",
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    userEmail: User.Identity?.Name,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                // Update SiteSettings to use the selected email provider
                var siteSettings = await _siteSettingsService.GetSettingsAsync();
                siteSettings.EmailProvider = EmailInput.Provider switch
                {
                    "Resend" => EmailProvider.Resend,
                    "SMTP" => EmailProvider.Smtp,
                    _ => EmailProvider.None
                };
                siteSettings.EmailFromAddress = EmailInput.FromAddress ?? siteSettings.EmailFromAddress;
                siteSettings.EnableEmailNotifications = EmailInput.Provider != "None";
                await _siteSettingsService.UpdateSettingsAsync(siteSettings, User.Identity?.Name);

                SuccessMessage = $"Email configuration saved successfully! Provider set to: {EmailInput.Provider}";
                ActiveTab = "email";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Email configuration");
                ErrorMessage = $"Error saving Email configuration: {ex.Message}";
                ActiveTab = "email";
            }

            return RedirectToPage();
        }

        // Delete configuration
        public async Task<IActionResult> OnPostDeleteAsync(int configId)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(configId);
                if (config == null)
                {
                    ErrorMessage = "Configuration not found";
                    return RedirectToPage();
                }

                var apiType = config.ApiType.ToLower();

                await _apiConfigService.DeleteConfigurationAsync(
                    configId,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name,
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                SuccessMessage = $"{config.ApiType} configuration deleted successfully!";
                ActiveTab = apiType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API configuration");
                ErrorMessage = $"Error deleting configuration: {ex.Message}";
            }

            return RedirectToPage();
        }

        // Activate/Deactivate
        public async Task<IActionResult> OnPostToggleActiveAsync(int configId, bool isActive)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(configId);
                if (config == null)
                {
                    return new JsonResult(new { success = false, message = "Configuration not found" });
                }

                await _apiConfigService.SetActiveStatusAsync(
                    configId,
                    isActive,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name
                );

                return new JsonResult(new
                {
                    success = true,
                    message = $"Configuration {(isActive ? "activated" : "deactivated")} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling active status");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Test email configuration
        public async Task<IActionResult> OnPostTestEmailAsync(string toEmail)
        {
            try
            {
                // We need to inject IEmailService to test
                var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
                var result = await emailService.SendTestEmailAsync(toEmail);
                
                return new JsonResult(new
                {
                    success = result,
                    message = result 
                        ? $"Test email sent successfully to {toEmail}" 
                        : "Failed to send test email. Check logs for details."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Input Models
        public class StripeConfigInput
        {
            public string? PublishableKey { get; set; }
            public string? SecretKey { get; set; }
            public string? WebhookSecret { get; set; }
            public bool IsTestMode { get; set; } = true;
            public string? Description { get; set; }
        }

        public class CloudinaryConfigInput
        {
            public string? CloudName { get; set; }
            public string? ApiKey { get; set; }
            public string? ApiSecret { get; set; }
            public string? Description { get; set; }
        }

        public class ShippingApiConfigInput
        {
            public string? Key1 { get; set; }
            public string? Key2 { get; set; }
            public string? Key3 { get; set; }
            public string? Key4 { get; set; }
            public bool UseSandbox { get; set; }
            public string? Description { get; set; }
        }

        public class AIConfigInput
        {
            public string? ApiKey { get; set; }
            public string? SecondaryKey { get; set; }
            public string? Endpoint { get; set; }
            public string? Model { get; set; }
            public int MaxTokens { get; set; } = 2000;
            public bool Enabled { get; set; }
            public string? Description { get; set; }
        }

        public class EmailConfigInput
        {
            public string Provider { get; set; } = "None"; // None | Resend | SMTP
            public string? FromAddress { get; set; }
            // Resend
            public string? ResendApiKey { get; set; }
            public string? ResendDomain { get; set; }
            // SMTP
            public string? SmtpHost { get; set; }
            public int SmtpPort { get; set; } = 587;
            public string? SmtpUsername { get; set; }
            public string? SmtpPassword { get; set; }
            public bool SmtpUseSsl { get; set; } = true;
        }
    }
}
