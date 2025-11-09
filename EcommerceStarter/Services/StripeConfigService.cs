using EcommerceStarter.Data;
using EcommerceStarter.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceStarter.Services
{
    public interface IStripeConfigService
    {
        Task<string> GetPublishableKeyAsync();
        Task<string> GetSecretKeyAsync();
        Task<string> GetWebhookSecretAsync();
        Task<bool> IsConfiguredAsync();
    }

    public class StripeConfigService : IStripeConfigService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryption;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeConfigService> _logger;

        public StripeConfigService(
            ApplicationDbContext context,
            IEncryptionService encryption,
            IConfiguration configuration,
            ILogger<StripeConfigService> logger)
        {
            _context = context;
            _encryption = encryption;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetPublishableKeyAsync()
        {
            try
            {
                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();

                if (config != null && !string.IsNullOrEmpty(config.EncryptedPublishableKey))
                {
                    return _encryption.Decrypt(config.EncryptedPublishableKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting publishable key from database");
            }

            // Fallback to appsettings
            var fallback = _configuration["Stripe:PublishableKey"];
            if (!string.IsNullOrEmpty(fallback))
            {
                _logger.LogWarning("Using Stripe Publishable Key from appsettings (fallback)");
                return fallback;
            }

            throw new InvalidOperationException("Stripe Publishable Key not configured");
        }

        public async Task<string> GetSecretKeyAsync()
        {
            try
            {
                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();

                if (config != null && !string.IsNullOrEmpty(config.EncryptedSecretKey))
                {
                    return _encryption.Decrypt(config.EncryptedSecretKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secret key from database");
            }

            // Fallback to appsettings
            var fallback = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(fallback))
            {
                _logger.LogWarning("Using Stripe Secret Key from appsettings (fallback)");
                return fallback;
            }

            throw new InvalidOperationException("Stripe Secret Key not configured");
        }

        public async Task<string> GetWebhookSecretAsync()
        {
            try
            {
                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();

                if (config != null && !string.IsNullOrEmpty(config.EncryptedWebhookSecret))
                {
                    return _encryption.Decrypt(config.EncryptedWebhookSecret);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook secret from database");
            }

            // Fallback to appsettings
            var fallback = _configuration["Stripe:WebhookSecret"];
            if (!string.IsNullOrEmpty(fallback))
            {
                _logger.LogWarning("Using Stripe Webhook Secret from appsettings (fallback)");
                return fallback;
            }

            throw new InvalidOperationException("Stripe Webhook Secret not configured");
        }

        public async Task<bool> IsConfiguredAsync()
        {
            try
            {
                var config = await _context.StripeConfigurations.FirstOrDefaultAsync();
                return config != null 
                    && !string.IsNullOrEmpty(config.EncryptedPublishableKey)
                    && !string.IsNullOrEmpty(config.EncryptedSecretKey);
            }
            catch
            {
                return false;
            }
        }
    }
}
