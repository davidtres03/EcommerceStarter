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
        private const string STRIPE_API_TYPE = "Stripe";
        private const string STRIPE_LIVE_NAME = "Stripe-Live";
        private const string STRIPE_TEST_NAME = "Stripe-Test";
        
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

        /// <summary>
        /// Get the active Stripe configuration (prefers Live, falls back to Test)
        /// </summary>
        private async Task<ApiConfiguration?> GetActiveStripeConfigAsync()
        {
            // Try to get Live config first
            var config = await _context.ApiConfigurations
                .FirstOrDefaultAsync(ac => 
                    ac.ApiType == STRIPE_API_TYPE && 
                    ac.Name == STRIPE_LIVE_NAME && 
                    ac.IsActive);

            // Fall back to Test config
            if (config == null)
            {
                config = await _context.ApiConfigurations
                    .FirstOrDefaultAsync(ac => 
                        ac.ApiType == STRIPE_API_TYPE && 
                        ac.Name == STRIPE_TEST_NAME && 
                        ac.IsActive);
            }

            return config;
        }

        public async Task<string> GetPublishableKeyAsync()
        {
            try
            {
                var config = await GetActiveStripeConfigAsync();

                if (config != null && !string.IsNullOrEmpty(config.EncryptedValue1))
                {
                    try
                    {
                        return _encryption.Decrypt(config.EncryptedValue1);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting Stripe publishable key from ApiConfigurations");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Stripe publishable key from database");
            }

            // No appsettings fallback: enforce DB-only configuration
            throw new InvalidOperationException("Stripe Publishable Key not configured in database");
        }

        public async Task<string> GetSecretKeyAsync()
        {
            try
            {
                var config = await GetActiveStripeConfigAsync();

                if (config != null && !string.IsNullOrEmpty(config.EncryptedValue2))
                {
                    try
                    {
                        return _encryption.Decrypt(config.EncryptedValue2);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting Stripe secret key from ApiConfigurations");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Stripe secret key from database");
            }

            // No appsettings fallback: enforce DB-only configuration
            throw new InvalidOperationException("Stripe Secret Key not configured in database");
        }

        public async Task<string> GetWebhookSecretAsync()
        {
            try
            {
                var config = await GetActiveStripeConfigAsync();

                if (config != null && !string.IsNullOrEmpty(config.EncryptedValue3))
                {
                    try
                    {
                        return _encryption.Decrypt(config.EncryptedValue3);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error decrypting Stripe webhook secret from ApiConfigurations");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Stripe webhook secret from database");
            }

            // No appsettings fallback: enforce DB-only configuration
            throw new InvalidOperationException("Stripe Webhook Secret not configured in database");
        }

        public async Task<bool> IsConfiguredAsync()
        {
            try
            {
                var config = await GetActiveStripeConfigAsync();
                return config != null 
                    && !string.IsNullOrEmpty(config.EncryptedValue1)
                    && !string.IsNullOrEmpty(config.EncryptedValue2);
            }
            catch
            {
                return false;
            }
        }
    }
}
