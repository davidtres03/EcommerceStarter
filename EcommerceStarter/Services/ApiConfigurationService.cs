using System.Security.Claims;
using System.Text.Json;
using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Service for managing unified API configurations
    /// Handles encryption, decryption, audit logging, and configuration management
    /// </summary>
    public interface IApiConfigurationService
    {
        /// <summary>
        /// Get a configuration by type and name
        /// </summary>
        Task<ApiConfiguration?> GetConfigurationAsync(string apiType, string name);

        /// <summary>
        /// Get all active configurations of a specific type
        /// </summary>
        Task<List<ApiConfiguration>> GetConfigurationsByTypeAsync(string apiType, bool activeOnly = true);

        /// <summary>
        /// Get all configurations (with optional filtering)
        /// </summary>
        Task<List<ApiConfiguration>> GetAllConfigurationsAsync(bool activeOnly = true);

        /// <summary>
        /// Save or update a configuration
        /// </summary>
        Task<ApiConfiguration> SaveConfigurationAsync(
            string apiType,
            string name,
            Dictionary<string, string?> encryptedValues,
            string? metadata = null,
            string? description = null,
            bool isTestMode = false,
            string? userId = null,
            string? userEmail = null,
            string? ipAddress = null
        );

        /// <summary>
        /// Delete a configuration
        /// </summary>
        Task<bool> DeleteConfigurationAsync(int configurationId, string? userId = null, string? userEmail = null, string? ipAddress = null);

        /// <summary>
        /// Get decrypted value(s) from a configuration
        /// </summary>
        Task<Dictionary<string, string?>> GetDecryptedValuesAsync(int configurationId);

        /// <summary>
        /// Get a specific decrypted value by field name
        /// </summary>
        Task<string?> GetDecryptedValueAsync(int configurationId, string fieldName);

        /// <summary>
        /// Get audit logs for a configuration
        /// </summary>
        Task<List<ApiConfigurationAuditLog>> GetAuditLogsAsync(int configurationId, int limit = 50);

        /// <summary>
        /// Mark a configuration as tested/validated
        /// </summary>
        Task<bool> MarkAsTestedAsync(int configurationId, string testStatus, string? notes = null, string? userId = null, string? userEmail = null);

        /// <summary>
        /// Activate or deactivate a configuration
        /// </summary>
        Task<bool> SetActiveStatusAsync(int configurationId, bool isActive, string? userId = null, string? userEmail = null);
    }

    public class ApiConfigurationService : IApiConfigurationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<ApiConfigurationService> _logger;

        public ApiConfigurationService(
            ApplicationDbContext context,
            IEncryptionService encryption,
            ILogger<ApiConfigurationService> logger)
        {
            _context = context;
            _encryption = encryption;
            _logger = logger;
        }

        /// <summary>
        /// Get a configuration by type and name
        /// </summary>
        public async Task<ApiConfiguration?> GetConfigurationAsync(string apiType, string name)
        {
            return await _context.ApiConfigurations
                .Include(ac => ac.AuditLogs)
                .FirstOrDefaultAsync(ac => ac.ApiType == apiType && ac.Name == name);
        }

        /// <summary>
        /// Get all active configurations of a specific type
        /// </summary>
        public async Task<List<ApiConfiguration>> GetConfigurationsByTypeAsync(string apiType, bool activeOnly = true)
        {
            var query = _context.ApiConfigurations
                .Include(ac => ac.AuditLogs)
                .Where(ac => ac.ApiType == apiType);

            if (activeOnly)
            {
                query = query.Where(ac => ac.IsActive);
            }

            return await query.OrderByDescending(ac => ac.LastUpdated).ToListAsync();
        }

        /// <summary>
        /// Get all configurations
        /// </summary>
        public async Task<List<ApiConfiguration>> GetAllConfigurationsAsync(bool activeOnly = true)
        {
            var query = _context.ApiConfigurations
                .Include(ac => ac.AuditLogs)
                .AsQueryable();

            if (activeOnly)
            {
                query = query.Where(ac => ac.IsActive);
            }

            return await query.OrderBy(ac => ac.ApiType).ThenBy(ac => ac.Name).ToListAsync();
        }

        /// <summary>
        /// Save or update a configuration with encryption
        /// </summary>
        public async Task<ApiConfiguration> SaveConfigurationAsync(
            string apiType,
            string name,
            Dictionary<string, string?> encryptedValues,
            string? metadata = null,
            string? description = null,
            bool isTestMode = false,
            string? userId = null,
            string? userEmail = null,
            string? ipAddress = null)
        {
            try
            {
                var existingConfig = await _context.ApiConfigurations
                    .FirstOrDefaultAsync(ac => ac.ApiType == apiType && ac.Name == name);

                var isNew = existingConfig == null;
                var config = existingConfig ?? new ApiConfiguration
                {
                    ApiType = apiType,
                    Name = name,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                var changes = new Dictionary<string, string>();

                // Encrypt and assign values - ONLY if a new value is provided
                // If the value is empty, preserve the existing encrypted value
                if (encryptedValues.ContainsKey("Value1") && !string.IsNullOrEmpty(encryptedValues["Value1"]))
                {
                    config.EncryptedValue1 = _encryption.Encrypt(encryptedValues["Value1"]);
                    changes["Value1"] = "Updated";
                }

                if (encryptedValues.ContainsKey("Value2") && !string.IsNullOrEmpty(encryptedValues["Value2"]))
                {
                    config.EncryptedValue2 = _encryption.Encrypt(encryptedValues["Value2"]);
                    changes["Value2"] = "Updated";
                }

                if (encryptedValues.ContainsKey("Value3") && !string.IsNullOrEmpty(encryptedValues["Value3"]))
                {
                    config.EncryptedValue3 = _encryption.Encrypt(encryptedValues["Value3"]);
                    changes["Value3"] = "Updated";
                }

                if (encryptedValues.ContainsKey("Value4") && !string.IsNullOrEmpty(encryptedValues["Value4"]))
                {
                    config.EncryptedValue4 = _encryption.Encrypt(encryptedValues["Value4"]);
                    changes["Value4"] = "Updated";
                }

                if (encryptedValues.ContainsKey("Value5") && !string.IsNullOrEmpty(encryptedValues["Value5"]))
                {
                    config.EncryptedValue5 = _encryption.Encrypt(encryptedValues["Value5"]);
                    changes["Value5"] = "Updated";
                }

                // Update metadata only if provided
                if (!string.IsNullOrEmpty(metadata))
                {
                    config.MetadataJson = metadata;
                    changes["Metadata"] = "Updated";
                }

                // Update basic properties - only if changed
                if (description != config.Description)
                {
                    config.Description = description;
                    changes["Description"] = "Updated";
                }

                if (isTestMode != config.IsTestMode)
                {
                    config.IsTestMode = isTestMode;
                    changes["IsTestMode"] = isTestMode ? "Updated to Test" : "Updated to Live";
                }

                config.LastUpdated = DateTime.UtcNow;
                config.UpdatedBy = userId;

                if (isNew)
                {
                    _context.ApiConfigurations.Add(config);
                }

                await _context.SaveChangesAsync();

                // Log audit
                await LogAuditAsync(config.Id, isNew ? "Created" : "Updated", JsonSerializer.Serialize(changes), userId, userEmail, ipAddress);

                _logger.LogInformation(
                    "{Action} API configuration: {ApiType}/{Name} by {User}",
                    isNew ? "Created" : "Updated",
                    apiType,
                    name,
                    userEmail ?? userId ?? "System"
                );

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API configuration: {ApiType}/{Name}", apiType, name);
                throw;
            }
        }

        /// <summary>
        /// Delete a configuration
        /// </summary>
        public async Task<bool> DeleteConfigurationAsync(int configurationId, string? userId = null, string? userEmail = null, string? ipAddress = null)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(configurationId);
                if (config == null)
                {
                    _logger.LogWarning("Configuration not found: {ConfigurationId}", configurationId);
                    return false;
                }

                await LogAuditAsync(configurationId, "Deleted", $"Deleted: {config.ApiType}/{config.Name}", userId, userEmail, ipAddress);

                _context.ApiConfigurations.Remove(config);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Deleted API configuration: {ApiType}/{Name} by {User}",
                    config.ApiType,
                    config.Name,
                    userEmail ?? userId ?? "System"
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API configuration: {ConfigurationId}", configurationId);
                throw;
            }
        }

        /// <summary>
        /// Get decrypted values from a configuration
        /// </summary>
        public async Task<Dictionary<string, string?>> GetDecryptedValuesAsync(int configurationId)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(configurationId);
                if (config == null)
                {
                    return new Dictionary<string, string?>();
                }

                var decrypted = new Dictionary<string, string?>
                {
                    { "Value1", string.IsNullOrEmpty(config.EncryptedValue1) ? null : _encryption.Decrypt(config.EncryptedValue1) },
                    { "Value2", string.IsNullOrEmpty(config.EncryptedValue2) ? null : _encryption.Decrypt(config.EncryptedValue2) },
                    { "Value3", string.IsNullOrEmpty(config.EncryptedValue3) ? null : _encryption.Decrypt(config.EncryptedValue3) },
                    { "Value4", string.IsNullOrEmpty(config.EncryptedValue4) ? null : _encryption.Decrypt(config.EncryptedValue4) },
                    { "Value5", string.IsNullOrEmpty(config.EncryptedValue5) ? null : _encryption.Decrypt(config.EncryptedValue5) }
                };

                return decrypted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting API configuration values: {ConfigurationId}", configurationId);
                throw;
            }
        }

        /// <summary>
        /// Get a specific decrypted value
        /// </summary>
        public async Task<string?> GetDecryptedValueAsync(int configurationId, string fieldName)
        {
            var values = await GetDecryptedValuesAsync(configurationId);
            return values.TryGetValue(fieldName, out var value) ? value : null;
        }

        /// <summary>
        /// Get audit logs for a configuration
        /// </summary>
        public async Task<List<ApiConfigurationAuditLog>> GetAuditLogsAsync(int configurationId, int limit = 50)
        {
            return await _context.ApiConfigurationAuditLogs
                .Where(aal => aal.ApiConfigurationId == configurationId)
                .OrderByDescending(aal => aal.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Mark as tested
        /// </summary>
        public async Task<bool> MarkAsTestedAsync(int configurationId, string testStatus, string? notes = null, string? userId = null, string? userEmail = null)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(configurationId);
                if (config == null)
                {
                    return false;
                }

                config.LastValidated = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await LogAuditAsync(
                    configurationId,
                    "Tested",
                    JsonSerializer.Serialize(new { testStatus, notes }),
                    userId,
                    userEmail,
                    null,
                    testStatus,
                    notes
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking configuration as tested: {ConfigurationId}", configurationId);
                throw;
            }
        }

        /// <summary>
        /// Set active status
        /// </summary>
        public async Task<bool> SetActiveStatusAsync(int configurationId, bool isActive, string? userId = null, string? userEmail = null)
        {
            try
            {
                var config = await _context.ApiConfigurations.FindAsync(configurationId);
                if (config == null)
                {
                    return false;
                }

                var oldStatus = config.IsActive;
                config.IsActive = isActive;
                config.LastUpdated = DateTime.UtcNow;
                config.UpdatedBy = userId;

                await _context.SaveChangesAsync();

                await LogAuditAsync(
                    configurationId,
                    isActive ? "Activated" : "Deactivated",
                    JsonSerializer.Serialize(new { oldStatus, newStatus = isActive }),
                    userId,
                    userEmail
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active status for configuration: {ConfigurationId}", configurationId);
                throw;
            }
        }

        /// <summary>
        /// Log audit event
        /// </summary>
        private async Task LogAuditAsync(
            int configurationId,
            string action,
            string? changes = null,
            string? userId = null,
            string? userEmail = null,
            string? ipAddress = null,
            string? testStatus = null,
            string? notes = null)
        {
            try
            {
                var audit = new ApiConfigurationAuditLog
                {
                    ApiConfigurationId = configurationId,
                    Action = action,
                    Changes = changes,
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    UserEmail = userEmail,
                    IpAddress = ipAddress,
                    TestStatus = testStatus,
                    Notes = notes
                };

                _context.ApiConfigurationAuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging API configuration audit");
                // Don't throw - audit failure shouldn't block the main action
            }
        }
    }
}
