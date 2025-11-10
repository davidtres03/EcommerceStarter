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
                var existing = await _context.ApiKeySettings.FirstOrDefaultAsync();

                if (existing == null)
                {
                    // Encrypt sensitive fields
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

                    settings.LastUpdated = DateTime.UtcNow;
                    settings.LastUpdatedBy = updatedBy;

                    _context.ApiKeySettings.Add(settings);
                }
                else
                {
                    // Update existing
                    existing.UspsUserId = settings.UspsUserId;
                    existing.UspsEnabled = settings.UspsEnabled;
                    existing.UpsClientId = settings.UpsClientId;
                    existing.UpsAccountNumber = settings.UpsAccountNumber;
                    existing.UpsEnabled = settings.UpsEnabled;
                    existing.FedExAccountNumber = settings.FedExAccountNumber;
                    existing.FedExMeterNumber = settings.FedExMeterNumber;
                    existing.FedExEnabled = settings.FedExEnabled;

                    // Only update encrypted fields if new value provided
                    if (!string.IsNullOrEmpty(settings.UspsPasswordEncrypted))
                    {
                        existing.UspsPasswordEncrypted = _encryptionService.Encrypt(settings.UspsPasswordEncrypted);
                    }

                    if (!string.IsNullOrEmpty(settings.UpsClientSecretEncrypted))
                    {
                        existing.UpsClientSecretEncrypted = _encryptionService.Encrypt(settings.UpsClientSecretEncrypted);
                    }

                    if (!string.IsNullOrEmpty(settings.FedExKeyEncrypted))
                    {
                        existing.FedExKeyEncrypted = _encryptionService.Encrypt(settings.FedExKeyEncrypted);
                    }

                    if (!string.IsNullOrEmpty(settings.FedExPasswordEncrypted))
                    {
                        existing.FedExPasswordEncrypted = _encryptionService.Encrypt(settings.FedExPasswordEncrypted);
                    }

                    existing.LastUpdated = DateTime.UtcNow;
                    existing.LastUpdatedBy = updatedBy;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"API key settings updated by {updatedBy}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API key settings");
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
    }
}
