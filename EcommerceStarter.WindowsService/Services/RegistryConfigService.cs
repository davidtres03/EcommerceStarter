using Microsoft.Win32;

namespace EcommerceStarter.WindowsService.Services;

/// <summary>
/// Service for reading configuration from Windows Registry
/// Allows Windows Service to use same configuration as main application
/// </summary>
public class RegistryConfigService
{
    private readonly ILogger<RegistryConfigService> _logger;
    private const string REGISTRY_BASE_PATH = @"SOFTWARE\EcommerceStarter";

    public RegistryConfigService(ILogger<RegistryConfigService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the EcommerceStarter base URL from registry
    /// Falls back to configuration or localhost if not found
    /// </summary>
    public string GetBaseUrl(string fallbackUrl = "http://localhost:8080")
    {
        try
        {
            // Try to find any installation registry key
            using var baseKey = Registry.LocalMachine.OpenSubKey(REGISTRY_BASE_PATH);
            if (baseKey == null)
            {
                _logger.LogWarning("Registry path not found: {Path}. Using fallback URL: {Url}", REGISTRY_BASE_PATH, fallbackUrl);
                return fallbackUrl;
            }

            // Get the first site (most installations will have only one)
            var siteNames = baseKey.GetSubKeyNames();
            if (siteNames.Length == 0)
            {
                _logger.LogWarning("No site configurations found in registry. Using fallback URL: {Url}", fallbackUrl);
                return fallbackUrl;
            }

            var siteName = siteNames[0];
            using var siteKey = baseKey.OpenSubKey(siteName);
            if (siteKey == null)
            {
                _logger.LogWarning("Could not open site registry key: {Site}. Using fallback URL: {Url}", siteName, fallbackUrl);
                return fallbackUrl;
            }

            // Read ServiceUrl from registry (preferred) or BaseUrl (legacy fallback)
            var baseUrl = siteKey.GetValue("ServiceUrl")?.ToString();
            if (string.IsNullOrEmpty(baseUrl))
            {
                // Try legacy BaseUrl key for backwards compatibility
                baseUrl = siteKey.GetValue("BaseUrl")?.ToString();
            }
            
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogInformation("ServiceUrl not configured in registry for site: {Site}. Using fallback URL: {Url}", siteName, fallbackUrl);
                return fallbackUrl;
            }

            _logger.LogInformation("Using ServiceUrl from registry: {Url} (Site: {Site})", baseUrl, siteName);
            return baseUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading BaseUrl from registry. Using fallback URL: {Url}", fallbackUrl);
            return fallbackUrl;
        }
    }

    /// <summary>
    /// Get the site name from registry (first installation found)
    /// </summary>
    public string? GetSiteName()
    {
        try
        {
            using var baseKey = Registry.LocalMachine.OpenSubKey(REGISTRY_BASE_PATH);
            if (baseKey == null) return null;

            var siteNames = baseKey.GetSubKeyNames();
            return siteNames.Length > 0 ? siteNames[0] : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading site name from registry");
            return null;
        }
    }

    /// <summary>
    /// Get configuration value from registry for a specific site
    /// </summary>
    public string? GetConfigValue(string siteName, string valueName)
    {
        try
        {
            var path = $@"{REGISTRY_BASE_PATH}\{siteName}";
            using var key = Registry.LocalMachine.OpenSubKey(path);
            return key?.GetValue(valueName)?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading {ValueName} from registry for site {Site}", valueName, siteName);
            return null;
        }
    }

    /// <summary>
    /// Check if EcommerceStarter is installed (registry key exists)
    /// </summary>
    public bool IsInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(REGISTRY_BASE_PATH);
            return key != null;
        }
        catch
        {
            return false;
        }
    }
}
