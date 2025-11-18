using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for managing API keys for carrier tracking
    /// Encrypts sensitive data before storage
    /// </summary>
    public interface IApiKeyService
    {
        Task<ApiKeySettings> GetApiKeySettingsAsync();
        Task<bool> SaveApiKeySettingsAsync(ApiKeySettings settings, string updatedBy);
        Task<string?> GetUspsUserIdAsync();
        Task<string?> GetUspsPasswordAsync();
        Task<string?> GetUpsClientIdAsync();
        Task<string?> GetUpsClientSecretAsync();
        Task<string?> GetUpsAccountNumberAsync();
        Task<string?> GetFedExAccountNumberAsync();
        Task<string?> GetFedExMeterNumberAsync();
        Task<string?> GetFedExKeyAsync();
        Task<string?> GetFedExPasswordAsync();
        Task<string?> GetClaudeApiKeyAsync();
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<ApiKeyService> _logger;

        public ApiKeyService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            ILogger<ApiKeyService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<ApiKeySettings> GetApiKeySettingsAsync()
        {
            var settings = await _context.ApiKeySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings
                settings = new ApiKeySettings();
                _context.ApiKeySettings.Add(settings);
                await _context.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<bool> SaveApiKeySettingsAsync(ApiKeySettings settings, string updatedBy)
        {
            try
            {
                _logger.LogInformation("SaveApiKeySettingsAsync called with updatedBy={updatedBy}", updatedBy);

                var existing = await _context.ApiKeySettings.FirstOrDefaultAsync();
                _logger.LogInformation("Existing settings found: {existingFound}", existing != null);

                if (existing == null)
                {
                    _logger.LogInformation("Creating new API key settings");
                    // Encrypt sensitive fields (only if provided - they're coming from form, not encrypted yet)
                    if (!string.IsNullOrEmpty(settings.UspsPasswordEncrypted))
                    {
                        settings.UspsPasswordEncrypted = _encryptionService.Encrypt(settings.UspsPasswordEncrypted);
                    }

                    if (!string.IsNullOrEmpty(settings.UpsClientSecretEncrypted))
                    {
                        settings.UpsClientSecretEncrypted = _encryptionService.Encrypt(settings.UpsClientSecretEncrypted);
                    }

                    if (!string.IsNullOrEmpty(settings.FedExKeyEncrypted))
                    {
                        settings.FedExKeyEncrypted = _encryptionService.Encrypt(settings.FedExKeyEncrypted);
                    }

                    if (!string.IsNullOrEmpty(settings.FedExPasswordEncrypted))
                    {
                        settings.FedExPasswordEncrypted = _encryptionService.Encrypt(settings.FedExPasswordEncrypted);
                    }

                    // Encrypt AI API keys
                    if (!string.IsNullOrEmpty(settings.ClaudeApiKeyEncrypted))
                    {
                        settings.ClaudeApiKeyEncrypted = _encryptionService.Encrypt(settings.ClaudeApiKeyEncrypted);
                    }

                    settings.LastUpdated = DateTime.UtcNow;
                    settings.LastUpdatedBy = updatedBy;

                    _context.ApiKeySettings.Add(settings);
                }
                else
                {
                    _logger.LogInformation("Updating existing API key settings. Current Claude enabled: {claudeEnabled}, New Claude enabled: {newClaudeEnabled}", existing.ClaudeEnabled, settings.ClaudeEnabled);

                    // Update existing - non-encrypted fields
                    existing.UspsUserId = settings.UspsUserId;
                    existing.UspsEnabled = settings.UspsEnabled;
                    existing.UpsClientId = settings.UpsClientId;
                    existing.UpsAccountNumber = settings.UpsAccountNumber;
                    existing.UpsEnabled = settings.UpsEnabled;
                    existing.FedExAccountNumber = settings.FedExAccountNumber;
                    existing.FedExMeterNumber = settings.FedExMeterNumber;
                    existing.FedExEnabled = settings.FedExEnabled;

                    // Encrypted fields: ONLY encrypt if the value is NEW (from form), not if it's already encrypted from the database
                    // If the value looks like encrypted data (base64 from DB), leave it as-is
                    // If the value is plaintext (from form), encrypt it
                    if (!string.IsNullOrEmpty(settings.UspsPasswordEncrypted) && settings.UspsPasswordEncrypted != existing.UspsPasswordEncrypted)
                    {
                        // Value changed - encrypt only if it looks like plaintext (not already encrypted)
                        if (!IsLikelyEncrypted(settings.UspsPasswordEncrypted))
                        {
                            existing.UspsPasswordEncrypted = _encryptionService.Encrypt(settings.UspsPasswordEncrypted);
                        }
                        else
                        {
                            existing.UspsPasswordEncrypted = settings.UspsPasswordEncrypted;
                        }
                    }

                    if (!string.IsNullOrEmpty(settings.UpsClientSecretEncrypted) && settings.UpsClientSecretEncrypted != existing.UpsClientSecretEncrypted)
                    {
                        if (!IsLikelyEncrypted(settings.UpsClientSecretEncrypted))
                        {
                            existing.UpsClientSecretEncrypted = _encryptionService.Encrypt(settings.UpsClientSecretEncrypted);
                        }
                        else
                        {
                            existing.UpsClientSecretEncrypted = settings.UpsClientSecretEncrypted;
                        }
                    }

                    if (!string.IsNullOrEmpty(settings.FedExKeyEncrypted) && settings.FedExKeyEncrypted != existing.FedExKeyEncrypted)
                    {
                        if (!IsLikelyEncrypted(settings.FedExKeyEncrypted))
                        {
                            existing.FedExKeyEncrypted = _encryptionService.Encrypt(settings.FedExKeyEncrypted);
                        }
                        else
                        {
                            existing.FedExKeyEncrypted = settings.FedExKeyEncrypted;
                        }
                    }

                    if (!string.IsNullOrEmpty(settings.FedExPasswordEncrypted) && settings.FedExPasswordEncrypted != existing.FedExPasswordEncrypted)
                    {
                        if (!IsLikelyEncrypted(settings.FedExPasswordEncrypted))
                        {
                            existing.FedExPasswordEncrypted = _encryptionService.Encrypt(settings.FedExPasswordEncrypted);
                        }
                        else
                        {
                            existing.FedExPasswordEncrypted = settings.FedExPasswordEncrypted;
                        }
                    }

                    // Claude API key
                    if (!string.IsNullOrEmpty(settings.ClaudeApiKeyEncrypted) && settings.ClaudeApiKeyEncrypted != existing.ClaudeApiKeyEncrypted)
                    {
                        if (!IsLikelyEncrypted(settings.ClaudeApiKeyEncrypted))
                        {
                            existing.ClaudeApiKeyEncrypted = _encryptionService.Encrypt(settings.ClaudeApiKeyEncrypted);
                        }
                        else
                        {
                            existing.ClaudeApiKeyEncrypted = settings.ClaudeApiKeyEncrypted;
                        }
                    }

                    // AI Configuration - Update other fields
                    existing.ClaudeEnabled = settings.ClaudeEnabled;
                    existing.ClaudeModel = settings.ClaudeModel;
                    existing.ClaudeMaxTokens = settings.ClaudeMaxTokens;
                    existing.OllamaEnabled = settings.OllamaEnabled;
                    existing.OllamaEndpoint = settings.OllamaEndpoint;
                    existing.OllamaModel = settings.OllamaModel;
                    existing.AIPreferredBackend = settings.AIPreferredBackend;
                    existing.AIMaxCostPerRequest = settings.AIMaxCostPerRequest;
                    existing.AIEnableFallback = settings.AIEnableFallback;

                    existing.LastUpdated = DateTime.UtcNow;
                    existing.LastUpdatedBy = updatedBy;
                }

                _logger.LogInformation("About to call SaveChangesAsync");
                int changesCount = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync completed. Changes saved: {changesCount}", changesCount);

                _logger.LogInformation($"API key settings updated by {updatedBy}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API key settings: {message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Determines if a string looks like it's already encrypted (base64 encoded).
        /// Encrypted values are typically 50+ characters and contain only valid base64 characters.
        /// </summary>
        private static bool IsLikelyEncrypted(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 50)
                return false;

            try
            {
                // Try to decode as base64 - if it works, it's likely encrypted
                Convert.FromBase64String(value);
                // Additional check: encrypted values usually don't contain common plaintext patterns
                // and are mostly alphanumeric + /+ characters
                var base64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
                return value.All(c => base64Chars.Contains(c));
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetUspsUserIdAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            return settings.UspsUserId;
        }

        public async Task<string?> GetUspsPasswordAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            if (string.IsNullOrEmpty(settings.UspsPasswordEncrypted))
                return null;

            try
            {
                return _encryptionService.Decrypt(settings.UspsPasswordEncrypted);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUpsClientIdAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            return settings.UpsClientId;
        }

        public async Task<string?> GetUpsClientSecretAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            if (string.IsNullOrEmpty(settings.UpsClientSecretEncrypted))
                return null;

            try
            {
                return _encryptionService.Decrypt(settings.UpsClientSecretEncrypted);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetUpsAccountNumberAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            return settings.UpsAccountNumber;
        }

        public async Task<string?> GetFedExAccountNumberAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            return settings.FedExAccountNumber;
        }

        public async Task<string?> GetFedExMeterNumberAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            return settings.FedExMeterNumber;
        }

        public async Task<string?> GetFedExKeyAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            if (string.IsNullOrEmpty(settings.FedExKeyEncrypted))
                return null;

            try
            {
                return _encryptionService.Decrypt(settings.FedExKeyEncrypted);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> GetFedExPasswordAsync()
        {
            var settings = await GetApiKeySettingsAsync();
            if (string.IsNullOrEmpty(settings.FedExPasswordEncrypted))
                return null;

            try
            {
                return _encryptionService.Decrypt(settings.FedExPasswordEncrypted);
            }
            catch
            {
                return null;
            }
        }

        // AI Configuration Methods - Updated to use new ApiConfigurations table
        public async Task<string?> GetClaudeApiKeyAsync()
        {
            try
            {
                // Get active Claude configuration from new unified ApiConfigurations table
                var claudeConfig = await _context.ApiConfigurations
                    .Where(ac => ac.ApiType == "Claude" && ac.IsActive)
                    .FirstOrDefaultAsync();

                if (claudeConfig == null || string.IsNullOrEmpty(claudeConfig.EncryptedValue1))
                {
                    _logger.LogWarning("No active Claude API configuration found in ApiConfigurations table");
                    return null;
                }

                // Value1 = API Key for Claude
                return _encryptionService.Decrypt(claudeConfig.EncryptedValue1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Claude API key from ApiConfigurations");
                return null;
            }
        }
    }
}
