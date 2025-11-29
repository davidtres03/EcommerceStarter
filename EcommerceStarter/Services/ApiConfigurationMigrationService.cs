using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Services;

/// <summary>
/// Service to migrate API configuration data from legacy tables to normalized structure
/// Migrates from StripeConfigurations and APIKeySettings to ApiProviders/ApiSettings
/// </summary>
public class ApiConfigurationMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiConfigurationMigrationService> _logger;

    public ApiConfigurationMigrationService(
        ApplicationDbContext context,
        ILogger<ApiConfigurationMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Migrate all API configurations to normalized structure
    /// </summary>
    public async Task<MigrationResult> MigrateAsync()
    {
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("[ApiMigration] Starting API configuration migration");

            // Check if migration already performed
            if (await _context.ApiProviders.AnyAsync())
            {
                _logger.LogInformation("[ApiMigration] Migration already performed - providers exist");
                result.Success = true;
                result.Message = "Migration already completed";
                return result;
            }

            // Create providers first
            await CreateProvidersAsync(result);

            // Migrate Stripe configuration
            await MigrateStripeConfigurationAsync(result);

            // Migrate API Key Settings (shipping carriers and AI services)
            await MigrateApiKeySettingsAsync(result);

            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Migration completed successfully. Migrated {result.SettingsMigrated} settings across {result.ProvidersCreated} providers.";
            _logger.LogInformation("[ApiMigration] {Message}", result.Message);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Migration failed: {ex.Message}";
            _logger.LogError(ex, "[ApiMigration] Migration failed");
        }

        return result;
    }

    private async Task CreateProvidersAsync(MigrationResult result)
    {
        var providers = new List<ApiProvider>
        {
            new() 
            { 
                Code = "Stripe", 
                Name = "Stripe", 
                Category = "Payment", 
                WebsiteUrl = "https://stripe.com", 
                BaseEndpoint = "https://api.stripe.com", 
                IsActive = true 
            },
            new() 
            { 
                Code = "USPS", 
                Name = "USPS", 
                Category = "Shipping", 
                WebsiteUrl = "https://www.usps.com", 
                BaseEndpoint = "https://secure.shippingapis.com", 
                IsActive = true 
            },
            new() 
            { 
                Code = "UPS", 
                Name = "UPS", 
                Category = "Shipping", 
                WebsiteUrl = "https://www.ups.com", 
                BaseEndpoint = "https://onlinetools.ups.com", 
                IsActive = true 
            },
            new() 
            { 
                Code = "FedEx", 
                Name = "FedEx", 
                Category = "Shipping", 
                WebsiteUrl = "https://www.fedex.com", 
                BaseEndpoint = "https://apis.fedex.com", 
                IsActive = true 
            },
            new() 
            { 
                Code = "Claude", 
                Name = "Anthropic Claude", 
                Category = "AI", 
                WebsiteUrl = "https://anthropic.com", 
                BaseEndpoint = "https://api.anthropic.com", 
                IsActive = true 
            },
            new() 
            { 
                Code = "Ollama", 
                Name = "Ollama", 
                Category = "AI", 
                WebsiteUrl = "https://ollama.ai", 
                BaseEndpoint = null, 
                IsActive = true 
            }
        };

        await _context.ApiProviders.AddRangeAsync(providers);
        await _context.SaveChangesAsync(); // Save to get IDs

        result.ProvidersCreated = providers.Count;
        _logger.LogInformation("[ApiMigration] Created {Count} API providers", providers.Count);
    }

    private async Task MigrateStripeConfigurationAsync(MigrationResult result)
    {
        var stripeConfig = await _context.StripeConfigurations.FirstOrDefaultAsync();
        if (stripeConfig == null)
        {
            _logger.LogInformation("[ApiMigration] No Stripe configuration to migrate");
            return;
        }

        var stripeProvider = await _context.ApiProviders.FirstAsync(p => p.Code == "Stripe");

        var settings = new List<ApiSetting>
        {
            new()
            {
                ApiProviderId = stripeProvider.Id,
                SettingKey = "PublishableKey",
                EncryptedValue = stripeConfig.EncryptedPublishableKey,
                ValueType = "String",
                IsTestMode = stripeConfig.IsTestMode,
                IsEnabled = true,
                Description = "Stripe publishable key for client-side operations",
                UpdatedBy = stripeConfig.UpdatedBy,
                LastUpdated = stripeConfig.LastUpdated,
                DisplayOrder = 1
            },
            new()
            {
                ApiProviderId = stripeProvider.Id,
                SettingKey = "SecretKey",
                EncryptedValue = stripeConfig.EncryptedSecretKey,
                ValueType = "String",
                IsTestMode = stripeConfig.IsTestMode,
                IsEnabled = true,
                Description = "Stripe secret key for server-side operations",
                UpdatedBy = stripeConfig.UpdatedBy,
                LastUpdated = stripeConfig.LastUpdated,
                DisplayOrder = 2
            },
            new()
            {
                ApiProviderId = stripeProvider.Id,
                SettingKey = "WebhookSecret",
                EncryptedValue = stripeConfig.EncryptedWebhookSecret,
                ValueType = "String",
                IsTestMode = stripeConfig.IsTestMode,
                IsEnabled = true,
                Description = "Stripe webhook endpoint secret for signature verification",
                UpdatedBy = stripeConfig.UpdatedBy,
                LastUpdated = stripeConfig.LastUpdated,
                DisplayOrder = 3
            }
        };

        await _context.ApiSettings.AddRangeAsync(settings);
        result.SettingsMigrated += settings.Count;
        _logger.LogInformation("[ApiMigration] Migrated {Count} Stripe settings", settings.Count);
    }

    private async Task MigrateApiKeySettingsAsync(MigrationResult result)
    {
        var apiKeys = await _context.ApiKeySettings.FirstOrDefaultAsync();
        if (apiKeys == null)
        {
            _logger.LogInformation("[ApiMigration] No API key settings to migrate");
            return;
        }

        var settings = new List<ApiSetting>();

        // Migrate USPS
        var uspsProvider = await _context.ApiProviders.FirstAsync(p => p.Code == "USPS");
        if (!string.IsNullOrEmpty(apiKeys.UspsUserId))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = uspsProvider.Id,
                SettingKey = "UserId",
                PlainValue = apiKeys.UspsUserId,
                ValueType = "String",
                IsTestMode = apiKeys.UspsUseSandbox,
                IsEnabled = apiKeys.UspsEnabled,
                Description = "USPS Web Tools User ID",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 1
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.UspsPasswordEncrypted))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = uspsProvider.Id,
                SettingKey = "Password",
                EncryptedValue = apiKeys.UspsPasswordEncrypted,
                ValueType = "String",
                IsTestMode = apiKeys.UspsUseSandbox,
                IsEnabled = apiKeys.UspsEnabled,
                Description = "USPS Web Tools Password",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 2
            });
        }

        // Migrate UPS
        var upsProvider = await _context.ApiProviders.FirstAsync(p => p.Code == "UPS");
        if (!string.IsNullOrEmpty(apiKeys.UpsClientId))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = upsProvider.Id,
                SettingKey = "ClientId",
                PlainValue = apiKeys.UpsClientId,
                ValueType = "String",
                IsEnabled = apiKeys.UpsEnabled,
                Description = "UPS OAuth Client ID",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 1
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.UpsClientSecretEncrypted))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = upsProvider.Id,
                SettingKey = "ClientSecret",
                EncryptedValue = apiKeys.UpsClientSecretEncrypted,
                ValueType = "String",
                IsEnabled = apiKeys.UpsEnabled,
                Description = "UPS OAuth Client Secret",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 2
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.UpsAccountNumber))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = upsProvider.Id,
                SettingKey = "AccountNumber",
                PlainValue = apiKeys.UpsAccountNumber,
                ValueType = "String",
                IsEnabled = apiKeys.UpsEnabled,
                Description = "UPS Account Number",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 3
            });
        }

        // Migrate FedEx
        var fedexProvider = await _context.ApiProviders.FirstAsync(p => p.Code == "FedEx");
        if (!string.IsNullOrEmpty(apiKeys.FedExAccountNumber))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = fedexProvider.Id,
                SettingKey = "AccountNumber",
                PlainValue = apiKeys.FedExAccountNumber,
                ValueType = "String",
                IsEnabled = apiKeys.FedExEnabled,
                Description = "FedEx Account Number",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 1
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.FedExMeterNumber))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = fedexProvider.Id,
                SettingKey = "MeterNumber",
                PlainValue = apiKeys.FedExMeterNumber,
                ValueType = "String",
                IsEnabled = apiKeys.FedExEnabled,
                Description = "FedEx Meter Number",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 2
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.FedExKeyEncrypted))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = fedexProvider.Id,
                SettingKey = "ApiKey",
                EncryptedValue = apiKeys.FedExKeyEncrypted,
                ValueType = "String",
                IsEnabled = apiKeys.FedExEnabled,
                Description = "FedEx API Key",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 3
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.FedExPasswordEncrypted))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = fedexProvider.Id,
                SettingKey = "Password",
                EncryptedValue = apiKeys.FedExPasswordEncrypted,
                ValueType = "String",
                IsEnabled = apiKeys.FedExEnabled,
                Description = "FedEx API Password",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 4
            });
        }

        // Migrate Claude AI
        var claudeProvider = await _context.ApiProviders.FirstAsync(p => p.Code == "Claude");
        if (!string.IsNullOrEmpty(apiKeys.ClaudeApiKeyEncrypted))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = claudeProvider.Id,
                SettingKey = "ApiKey",
                EncryptedValue = apiKeys.ClaudeApiKeyEncrypted,
                ValueType = "String",
                IsEnabled = apiKeys.ClaudeEnabled,
                Description = "Anthropic Claude API Key",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 1
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.ClaudeModel))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = claudeProvider.Id,
                SettingKey = "Model",
                PlainValue = apiKeys.ClaudeModel,
                ValueType = "String",
                IsEnabled = apiKeys.ClaudeEnabled,
                Description = "Claude model to use (e.g., claude-3-5-sonnet-20241022)",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 2
            });
        }
        settings.Add(new ApiSetting
        {
            ApiProviderId = claudeProvider.Id,
            SettingKey = "MaxTokens",
            PlainValue = apiKeys.ClaudeMaxTokens.ToString(),
            ValueType = "Int",
            IsEnabled = apiKeys.ClaudeEnabled,
            Description = "Maximum tokens per request",
            UpdatedBy = apiKeys.LastUpdatedBy,
            LastUpdated = apiKeys.LastUpdated,
            DisplayOrder = 3
        });

        // Migrate Ollama
        var ollamaProvider = await _context.ApiProviders.FirstAsync(p => p.Code == "Ollama");
        if (!string.IsNullOrEmpty(apiKeys.OllamaEndpoint))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = ollamaProvider.Id,
                SettingKey = "Endpoint",
                PlainValue = apiKeys.OllamaEndpoint,
                ValueType = "String",
                IsEnabled = apiKeys.OllamaEnabled,
                Description = "Ollama API endpoint URL",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 1
            });
        }
        if (!string.IsNullOrEmpty(apiKeys.OllamaModel))
        {
            settings.Add(new ApiSetting
            {
                ApiProviderId = ollamaProvider.Id,
                SettingKey = "Model",
                PlainValue = apiKeys.OllamaModel,
                ValueType = "String",
                IsEnabled = apiKeys.OllamaEnabled,
                Description = "Ollama model to use",
                UpdatedBy = apiKeys.LastUpdatedBy,
                LastUpdated = apiKeys.LastUpdated,
                DisplayOrder = 2
            });
        }

        // Add AI global settings (not provider-specific, but we'll store under general provider if needed)
        // For now, we'll skip these as they're cross-provider settings that might need a different approach

        await _context.ApiSettings.AddRangeAsync(settings);
        result.SettingsMigrated += settings.Count;
        _logger.LogInformation("[ApiMigration] Migrated {Count} API key settings", settings.Count);
    }
}

/// <summary>
/// Result of API configuration migration
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProvidersCreated { get; set; }
    public int SettingsMigrated { get; set; }
}
