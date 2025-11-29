using System.Text.Json;

namespace EcommerceStarter.Services.ApiManagers
{
    public class CloudinaryApiManager
    {
        private readonly IApiConfigurationService _configs;
        public CloudinaryApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<Dictionary<string, string?>> GetAsync(string name = "Default")
        {
            var list = await _configs.GetConfigurationsByTypeAsync("Cloudinary");
            var cfg = list.FirstOrDefault(c => c.Name == name) ?? list.FirstOrDefault();
            return cfg == null ? new() : await _configs.GetDecryptedValuesAsync(cfg.Id);
        }
    }

    public class StripeApiManager
    {
        private readonly IApiConfigurationService _configs;
        public StripeApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<Dictionary<string, string?>> GetAsync(string name = "Default")
        {
            var list = await _configs.GetConfigurationsByTypeAsync("Stripe");
            var cfg = list.FirstOrDefault(c => c.Name == name) ?? list.FirstOrDefault();
            return cfg == null ? new() : await _configs.GetDecryptedValuesAsync(cfg.Id);
        }
    }

    public class UspsApiManager
    {
        private readonly IApiConfigurationService _configs;
        public UspsApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<Dictionary<string, string?>> GetAsync(string name = "Default")
        {
            var list = await _configs.GetConfigurationsByTypeAsync("USPS");
            var cfg = list.FirstOrDefault(c => c.Name == name) ?? list.FirstOrDefault();
            return cfg == null ? new() : await _configs.GetDecryptedValuesAsync(cfg.Id);
        }
    }

    public class UpsApiManager
    {
        private readonly IApiConfigurationService _configs;
        public UpsApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<Dictionary<string, string?>> GetAsync(string name = "Default")
        {
            var list = await _configs.GetConfigurationsByTypeAsync("UPS");
            var cfg = list.FirstOrDefault(c => c.Name == name) ?? list.FirstOrDefault();
            return cfg == null ? new() : await _configs.GetDecryptedValuesAsync(cfg.Id);
        }
    }

    public class FedExApiManager
    {
        private readonly IApiConfigurationService _configs;
        public FedExApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<Dictionary<string, string?>> GetAsync(string name = "Default")
        {
            var list = await _configs.GetConfigurationsByTypeAsync("FedEx");
            var cfg = list.FirstOrDefault(c => c.Name == name) ?? list.FirstOrDefault();
            return cfg == null ? new() : await _configs.GetDecryptedValuesAsync(cfg.Id);
        }
    }

    public class AiServicesApiManager
    {
        private readonly IApiConfigurationService _configs;
        public AiServicesApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<Dictionary<string, string?>> GetAsync(string name = "Default")
        {
            var list = await _configs.GetConfigurationsByTypeAsync("AI");
            var cfg = list.FirstOrDefault(c => c.Name == name) ?? list.FirstOrDefault();
            return cfg == null ? new() : await _configs.GetDecryptedValuesAsync(cfg.Id);
        }
    }

    public class CloudflareApiManager
    {
        private readonly IApiConfigurationService _configs;
        public CloudflareApiManager(IApiConfigurationService configs) { _configs = configs; }
        public async Task<(string? ApiToken, string? ZoneId)> GetCredentialsAsync(CancellationToken ct = default)
        {
            var list = await _configs.GetConfigurationsByTypeAsync("Cloudflare");
            var cfg = list.FirstOrDefault();
            if (cfg == null) return (null, null);
            var values = await _configs.GetDecryptedValuesAsync(cfg.Id);
            values.TryGetValue("Value1", out var token);
            values.TryGetValue("Value2", out var zoneId);
            return (token, zoneId);
        }
    }
}
