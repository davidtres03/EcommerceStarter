using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    // Lazy wrapper that resolves the real IEmailService asynchronously on first use
    // Avoids blocking DI during startup.
    public class LazyEmailService : IEmailService
    {
        private readonly EmailServiceFactory _factory;
        private IEmailService? _inner;
        private readonly object _lock = new object();

        public LazyEmailService(EmailServiceFactory factory)
        {
            _factory = factory;
        }

        private async Task<IEmailService> GetInnerAsync()
        {
            if (_inner != null)
                return _inner;

            // Double-checked locking
            lock (_lock)
            {
                if (_inner != null)
                    return _inner;
            }

            var svc = await _factory.CreateEmailServiceAsync();

            lock (_lock)
            {
                if (_inner == null)
                    _inner = svc;
            }

            return _inner;
        }

        public async Task<bool> SendOrderConfirmationAsync(Order order) => await (await GetInnerAsync()).SendOrderConfirmationAsync(order);
        public async Task<bool> SendShippingNotificationAsync(Order order, string trackingNumber = "") => await (await GetInnerAsync()).SendShippingNotificationAsync(order, trackingNumber);
        public async Task<bool> SendPasswordResetAsync(string email, string resetLink) => await (await GetInnerAsync()).SendPasswordResetAsync(email, resetLink);
        public async Task<bool> SendEmailVerificationAsync(ApplicationUser user, string verificationLink) => await (await GetInnerAsync()).SendEmailVerificationAsync(user, verificationLink);
        public async Task<bool> SendWelcomeEmailAsync(ApplicationUser user) => await (await GetInnerAsync()).SendWelcomeEmailAsync(user);
        public async Task<bool> SendAdminOrderNotificationAsync(Order order) => await (await GetInnerAsync()).SendAdminOrderNotificationAsync(order);
        public async Task<bool> SendTestEmailAsync(string toEmail) => await (await GetInnerAsync()).SendTestEmailAsync(toEmail);
        public async Task<bool> IsConfiguredAsync() => await (await GetInnerAsync()).IsConfiguredAsync();
    }
}