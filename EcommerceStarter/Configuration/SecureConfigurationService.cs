using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
#pragma warning disable CA1416
using Microsoft.Win32;
#pragma warning restore CA1416

namespace EcommerceStarter.Configuration
{
    public interface ISecureConfigurationService
    {
        string? GetConnectionString();
    }

    public class SecureConfigurationService : ISecureConfigurationService
    {
        private readonly ILogger<SecureConfigurationService> _logger;

        public SecureConfigurationService(ILogger<SecureConfigurationService> logger)
        {
            _logger = logger;
        }

        public string? GetConnectionString()
        {
            // 1) Environment variable override
            var fromEnv = Environment.GetEnvironmentVariable("ECOMMERCESTARTER_CONNECTION_STRING");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                _logger.LogInformation("Loaded connection string from environment variable.");
                return fromEnv;
            }

            // 2) Windows Registry (encrypted or plain)
            try
            {
                var cs = ReadFromRegistry();
                if (!string.IsNullOrWhiteSpace(cs))
                {
                    _logger.LogInformation("Loaded connection string from Windows Registry.");
                    return cs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read connection string from Windows Registry");
            }

            // Note: appsettings.json is NOT used in production
            // All configuration comes from Windows Registry (encrypted)
            _logger.LogError("Connection string not found in environment variable or Windows Registry");
            return null;
        }

        private string? ReadFromRegistry()
        {
#if NET6_0_OR_GREATER
            // Determine site name from IIS Application Pool (matches registry key)
            var siteName = GetSiteNameFromIIS();
            
            _logger.LogInformation("Attempting to read configuration for site: {SiteName}", siteName ?? "(unknown)");

            string? ReadKey(RegistryKey root, string subKey, string valueName)
            {
                try
                {
                    using var key = root.OpenSubKey(subKey, false);
                    return key?.GetValue(valueName)?.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to read registry key: {SubKey}\\{ValueName}", subKey, valueName);
                    return null;
                }
            }

            // Try per-instance key first (NEW: v1.0.9+)
            if (!string.IsNullOrWhiteSpace(siteName))
            {
                var instancePath = $@"Software\EcommerceStarter\{siteName}";
                _logger.LogDebug("Checking per-instance registry key: HKLM\\{Path}", instancePath);
                
                var enc = ReadKey(Registry.LocalMachine, instancePath, "ConnectionStringEncrypted");
                if (!string.IsNullOrWhiteSpace(enc))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(enc);
                        var unprot = ProtectedData.Unprotect(bytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
                        _logger.LogInformation("Successfully decrypted connection string from per-instance registry");
                        return System.Text.Encoding.UTF8.GetString(unprot);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "DPAPI decrypt failed for per-instance registry connection string");
                    }
                }
            }

            // Legacy fallback: Try old shared key location (pre-v1.0.9)
            _logger.LogDebug("Checking legacy shared registry key");
            var plain = ReadKey(Registry.LocalMachine, @"Software\EcommerceStarter", "ConnectionString")
                        ?? ReadKey(Registry.CurrentUser, @"Software\EcommerceStarter", "ConnectionString");
            if (!string.IsNullOrWhiteSpace(plain))
            {
                _logger.LogInformation("Loaded connection string from legacy shared registry key (plaintext)");
                return plain;
            }

            var encLegacy = ReadKey(Registry.LocalMachine, @"Software\EcommerceStarter", "ConnectionStringEncrypted")
                      ?? ReadKey(Registry.CurrentUser, @"Software\EcommerceStarter", "ConnectionStringEncrypted");
            if (!string.IsNullOrWhiteSpace(encLegacy))
            {
                try
                {
                    var bytes = Convert.FromBase64String(encLegacy);
                    var unprot = ProtectedData.Unprotect(bytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
                    _logger.LogInformation("Loaded connection string from legacy shared registry key (encrypted)");
                    return System.Text.Encoding.UTF8.GetString(unprot);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "DPAPI decrypt failed for legacy registry connection string");
                }
            }
#endif
            return null;
        }

        private string? GetSiteNameFromIIS()
        {
            try
            {
                // Check all possible environment variables
                var appPoolId = Environment.GetEnvironmentVariable("APP_POOL_ID");
                var appPoolName = Environment.GetEnvironmentVariable("APP_POOL_NAME"); 
                var siteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
                
                _logger.LogDebug("Environment variables - APP_POOL_ID: '{AppPoolId}', APP_POOL_NAME: '{AppPoolName}', WEBSITE_SITE_NAME: '{SiteName}'", 
                    appPoolId ?? "(null)", appPoolName ?? "(null)", siteName ?? "(null)");

                // Try APP_POOL_ID first (set in web.config)
                if (!string.IsNullOrWhiteSpace(appPoolId))
                {
                    _logger.LogInformation("Using APP_POOL_ID: {AppPoolId}", appPoolId);
                    return appPoolId;
                }
                
                // Try APP_POOL_NAME (IIS built-in variable)
                if (!string.IsNullOrWhiteSpace(appPoolName))
                {
                    _logger.LogInformation("Using APP_POOL_NAME: {AppPoolName}", appPoolName);
                    return appPoolName;
                }

                // Try IIS site name (alternative)
                if (!string.IsNullOrWhiteSpace(siteName))
                {
                    _logger.LogInformation("Using WEBSITE_SITE_NAME: {SiteName}", siteName);
                    return siteName;
                }

                _logger.LogWarning("Could not detect IIS site name from any environment variables");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error detecting IIS site name");
                return null;
            }
        }
    }
}
