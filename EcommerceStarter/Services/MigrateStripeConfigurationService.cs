using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EcommerceStarter.Migrations
{
    /// <summary>
    /// Migration helper to move Stripe configuration data from legacy StripeConfiguration table
    /// to the new unified ApiConfiguration table.
    /// 
    /// This script should be run once during the upgrade process to consolidate API configs.
    /// </summary>
    public class MigrateStripeConfigurationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MigrateStripeConfigurationService> _logger;

        public MigrateStripeConfigurationService(
            ApplicationDbContext context,
            ILogger<MigrateStripeConfigurationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Migrate all Stripe configurations from StripeConfiguration to ApiConfiguration table.
        /// Handles encrypted values properly by preserving existing encrypted data.
        /// </summary>
        public async Task<MigrationResult> MigrateStripeConfigurationsAsync()
        {
            var result = new MigrationResult();

            try
            {
                _logger.LogInformation("Starting Stripe configuration migration...");

                // Get all Stripe configurations from legacy table
                var stripeConfigs = await _context.StripeConfigurations.ToListAsync();
                _logger.LogInformation("Found {Count} Stripe configuration(s) to migrate", stripeConfigs.Count);

                if (!stripeConfigs.Any())
                {
                    result.Success = true;
                    result.Message = "No Stripe configurations found to migrate";
                    _logger.LogInformation("No Stripe configurations found");
                    return result;
                }

                int migratedCount = 0;

                foreach (var legacyConfig in stripeConfigs)
                {
                    try
                    {
                        // Check if this configuration already exists in new table
                        var existingConfig = await _context.ApiConfigurations
                            .FirstOrDefaultAsync(ac =>
                                ac.ApiType == "Stripe" &&
                                ac.Name == (legacyConfig.IsTestMode ? "Stripe-Test" : "Stripe-Live"));

                        if (existingConfig != null)
                        {
                            _logger.LogWarning(
                                "Stripe configuration {Name} already exists in ApiConfiguration table, skipping",
                                existingConfig.Name);
                            result.SkippedCount++;
                            continue;
                        }

                        // Create new ApiConfiguration from legacy StripeConfiguration
                        var newConfig = new ApiConfiguration
                        {
                            ApiType = "Stripe",
                            Name = legacyConfig.IsTestMode ? "Stripe-Test" : "Stripe-Live",
                            IsActive = true, // Assume active configs are valid
                            IsTestMode = legacyConfig.IsTestMode,
                            // Note: Encrypted values are preserved from legacy table
                            EncryptedValue1 = legacyConfig.EncryptedPublishableKey,
                            EncryptedValue2 = legacyConfig.EncryptedSecretKey,
                            EncryptedValue3 = legacyConfig.EncryptedWebhookSecret,
                            EncryptedValue4 = null, // Reserved for future use
                            EncryptedValue5 = null, // Reserved for future use
                            MetadataJson = JsonSerializer.Serialize(new
                            {
                                migratedFrom = "StripeConfiguration",
                                legacyId = legacyConfig.Id,
                                migratedDate = DateTime.UtcNow
                            }),
                            Description = "Migrated from legacy StripeConfiguration table",
                            CreatedAt = legacyConfig.LastUpdated,
                            LastUpdated = DateTime.UtcNow,
                            CreatedBy = "System",
                            UpdatedBy = legacyConfig.UpdatedBy ?? "System"
                        };

                        _context.ApiConfigurations.Add(newConfig);
                        await _context.SaveChangesAsync();

                        // Try to migrate audit logs if StripeConfigurationAuditLogs table exists
                        try
                        {
                            var auditLogs = await _context.StripeConfigurationAuditLogs
                                .ToListAsync();

                            if (auditLogs.Any())
                            {
                                var migratedAuditLogs = auditLogs.Select(oldLog => new ApiConfigurationAuditLog
                                {
                                    ApiConfigurationId = newConfig.Id,
                                    Action = MapLegacyAction(oldLog.Action),
                                    Changes = oldLog.Changes ?? JsonSerializer.Serialize(new { note = "Migrated from legacy table" }),
                                    Timestamp = oldLog.Timestamp,
                                    UserId = oldLog.UserId,
                                    UserEmail = oldLog.UserEmail,
                                    IpAddress = oldLog.IpAddress,
                                    TestStatus = oldLog.WasTestMode ? "INFO" : null,
                                    Notes = $"Migrated from StripeConfigurationAuditLog (legacy ID: {oldLog.Id})"
                                }).ToList();

                                _context.ApiConfigurationAuditLogs.AddRange(migratedAuditLogs);
                                await _context.SaveChangesAsync();

                                _logger.LogInformation(
                                    "Migrated {Count} audit logs for Stripe config {Name}",
                                    auditLogs.Count,
                                    newConfig.Name);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not migrate audit logs - table may not exist yet");
                            // Continue migration without audit logs
                        }

                        migratedCount++;
                        _logger.LogInformation("Successfully migrated Stripe config: {Name}", newConfig.Name);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error migrating Stripe config: {ex.Message}");
                        _logger.LogError(ex, "Error migrating Stripe configuration");
                    }
                }

                result.Success = true;
                result.MigratedCount = migratedCount;
                result.Message = $"Successfully migrated {migratedCount} Stripe configuration(s)";

                _logger.LogInformation("Stripe configuration migration completed: {Message}", result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Fatal error during migration: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError(ex, "Fatal error during Stripe configuration migration");
            }

            return result;
        }

        /// <summary>
        /// Migrate all USPS/UPS/FedEx configurations from ApiKeySettings to ApiConfiguration table.
        /// </summary>
        public async Task<MigrationResult> MigrateShippingConfigurationsAsync()
        {
            var result = new MigrationResult();

            try
            {
                _logger.LogInformation("Starting shipping API configuration migration...");

                // Get existing ApiKeySettings
                var apiKeySettings = await _context.ApiKeySettings.FirstOrDefaultAsync();

                if (apiKeySettings == null)
                {
                    result.Success = true;
                    result.Message = "No ApiKeySettings found to migrate";
                    _logger.LogInformation("No ApiKeySettings found");
                    return result;
                }

                int migratedCount = 0;

                // Migrate USPS
                if (!string.IsNullOrEmpty(apiKeySettings.UspsUserId) && 
                    !await ConfigurationExists("USPS", "USPS-Production"))
                {
                    await CreateShippingConfiguration("USPS", "USPS-Production",
                        apiKeySettings.UspsUserId,
                        apiKeySettings.UspsPasswordEncrypted,
                        null,
                        null);
                    migratedCount++;
                }

                // Migrate UPS
                if (!string.IsNullOrEmpty(apiKeySettings.UpsClientId) && 
                    !await ConfigurationExists("UPS", "UPS-Production"))
                {
                    await CreateShippingConfiguration("UPS", "UPS-Production",
                        apiKeySettings.UpsClientId,
                        apiKeySettings.UpsClientSecretEncrypted,
                        apiKeySettings.UpsAccountNumber,
                        null);
                    migratedCount++;
                }

                // Migrate FedEx
                if (!string.IsNullOrEmpty(apiKeySettings.FedExAccountNumber) && 
                    !await ConfigurationExists("FedEx", "FedEx-Production"))
                {
                    await CreateShippingConfiguration("FedEx", "FedEx-Production",
                        apiKeySettings.FedExKeyEncrypted,
                        apiKeySettings.FedExPasswordEncrypted,
                        apiKeySettings.FedExAccountNumber,
                        apiKeySettings.FedExMeterNumber);
                    migratedCount++;
                }

                result.Success = true;
                result.MigratedCount = migratedCount;
                result.Message = $"Successfully migrated {migratedCount} shipping configuration(s)";

                _logger.LogInformation("Shipping configuration migration completed: {Message}", result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Fatal error during migration: {ex.Message}";
                result.Errors.Add(ex.ToString());
                _logger.LogError(ex, "Fatal error during shipping configuration migration");
            }

            return result;
        }

        private async Task<bool> ConfigurationExists(string apiType, string name)
        {
            return await _context.ApiConfigurations
                .AnyAsync(ac => ac.ApiType == apiType && ac.Name == name);
        }

        private async Task CreateShippingConfiguration(
            string apiType,
            string name,
            string? key1,
            string? key2,
            string? key3,
            string? key4)
        {
            // Note: These values would normally be encrypted by the service
            // For migration, we're storing them encrypted as they come from existing encrypted storage
            var newConfig = new ApiConfiguration
            {
                ApiType = apiType,
                Name = name,
                IsActive = true,
                IsTestMode = false,
                EncryptedValue1 = key1,
                EncryptedValue2 = key2,
                EncryptedValue3 = key3,
                EncryptedValue4 = key4,
                EncryptedValue5 = null,
                MetadataJson = JsonSerializer.Serialize(new
                {
                    migratedFrom = "ApiKeySettings",
                    migratedDate = DateTime.UtcNow
                }),
                Description = $"Migrated from legacy ApiKeySettings table",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                CreatedBy = "System",
                UpdatedBy = "System"
            };

            _context.ApiConfigurations.Add(newConfig);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully migrated {ApiType} configuration: {Name}", apiType, name);
        }

        private static string MapLegacyAction(string? legacyAction)
        {
            return legacyAction switch
            {
                "Created" => "Created",
                "Updated" => "Updated",
                "Deleted" => "Deleted",
                "Viewed" => "Viewed",
                "Tested" => "Tested",
                _ => "Updated"
            };
        }

        public class MigrationResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int MigratedCount { get; set; }
            public int SkippedCount { get; set; }
            public List<string> Errors { get; set; } = new();

            public override string ToString()
            {
                var summary = $"Success: {Success}\nMessage: {Message}\nMigrated: {MigratedCount}\nSkipped: {SkippedCount}";
                if (Errors.Any())
                {
                    summary += $"\nErrors:\n" + string.Join("\n", Errors);
                }
                return summary;
            }
        }
    }
}
