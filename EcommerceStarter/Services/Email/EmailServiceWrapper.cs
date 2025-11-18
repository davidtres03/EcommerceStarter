using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Wrapper that defers email service initialization until first use
    /// This prevents blocking during dependency injection and allows email provider switching
    /// </summary>
    public class EmailServiceWrapper : IEmailService
    {
        private readonly EmailServiceFactory _factory;
        private readonly ILogger<EmailServiceWrapper> _logger;
        private IEmailService? _innerService;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public EmailServiceWrapper(
            EmailServiceFactory factory,
            ILogger<EmailServiceWrapper> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        private async Task<IEmailService> GetServiceAsync()
        {
            if (_innerService != null)
                return _innerService;

            await _initLock.WaitAsync();
            try
            {
                if (_innerService == null)
                {
                    _logger.LogDebug("Lazy-initializing email service...");
                    _innerService = await _factory.CreateEmailServiceAsync();
                }
                return _innerService;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<bool> SendOrderConfirmationAsync(Order order)
        {
            var service = await GetServiceAsync();
            return await service.SendOrderConfirmationAsync(order);
        }

        public async Task<bool> SendShippingNotificationAsync(Order order, string trackingNumber = "")
        {
            var service = await GetServiceAsync();
            return await service.SendShippingNotificationAsync(order, trackingNumber);
        }

        public async Task<bool> SendPasswordResetAsync(string email, string resetLink)
        {
            var service = await GetServiceAsync();
            return await service.SendPasswordResetAsync(email, resetLink);
        }

        public async Task<bool> SendEmailVerificationAsync(ApplicationUser user, string verificationLink)
        {
            var service = await GetServiceAsync();
            return await service.SendEmailVerificationAsync(user, verificationLink);
        }

        public async Task<bool> SendWelcomeEmailAsync(ApplicationUser user)
        {
            var service = await GetServiceAsync();
            return await service.SendWelcomeEmailAsync(user);
        }

        public async Task<bool> SendAdminOrderNotificationAsync(Order order)
        {
            var service = await GetServiceAsync();
            return await service.SendAdminOrderNotificationAsync(order);
        }

        public async Task<bool> SendTestEmailAsync(string toEmail)
        {
            var service = await GetServiceAsync();
            return await service.SendTestEmailAsync(toEmail);
        }

        public async Task<bool> IsConfiguredAsync()
        {
            var service = await GetServiceAsync();
            return await service.IsConfiguredAsync();
        }
    }
}
