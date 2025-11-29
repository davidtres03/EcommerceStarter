using Microsoft.Win32;
using Microsoft.Extensions.Logging;

namespace EcommerceStarter.Services
{
    public interface IRegistryService
    {
        string? GetSiteName();
        string? GetString(string siteName, string valueName);
        int? GetInt(string siteName, string valueName);
    }

    public class RegistryService : IRegistryService
    {
        private readonly ILogger<RegistryService> _logger;
        private const string REGISTRY_BASE_PATH = @"SOFTWARE\\EcommerceStarter";

        public RegistryService(ILogger<RegistryService> logger)
        {
            _logger = logger;
        }

        public string? GetSiteName()
        {
            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(REGISTRY_BASE_PATH);
                var names = baseKey?.GetSubKeyNames();
                return (names != null && names.Length > 0) ? names[0] : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read site name from registry");
                return null;
            }
        }

        public string? GetString(string siteName, string valueName)
        {
            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(REGISTRY_BASE_PATH);
                using var siteKey = baseKey?.OpenSubKey(siteName);
                return siteKey?.GetValue(valueName)?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read {Value} from registry for site {Site}", valueName, siteName);
                return null;
            }
        }

        public int? GetInt(string siteName, string valueName)
        {
            var s = GetString(siteName, valueName);
            return int.TryParse(s, out var v) ? v : null;
        }
    }
}