using EcommerceStarter.Models;

namespace EcommerceStarter.Services
{
    /// <summary>
    /// Factory for creating email service instances based on configured provider
    /// Supports provider switching without code changes
    /// </summary>
    public class EmailServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISiteSettingsService _siteSettingsService;
        private readonly ILogger<EmailServiceFactory> _logger;

        public EmailServiceFactory(
            IServiceProvider serviceProvider,
            ISiteSettingsService siteSettingsService,
            ILogger<EmailServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _siteSettingsService = siteSettingsService;
            _logger = logger;
        }

        /// <summary>
        /// Creates the appropriate email service based on site settings
        /// </summary>
        public async Task<IEmailService> CreateEmailServiceAsync()
        {
            try
            {
                var settings = await _siteSettingsService.GetSettingsAsync();

                if (!settings.EnableEmailNotifications)
                {
                    _logger.LogInformation("Email notifications are disabled");
                    return new NullEmailService();
                }

                return settings.EmailProvider switch
                {
                    EmailProvider.Resend => _serviceProvider.GetRequiredService<ResendEmailService>(),
                    EmailProvider.Smtp => _serviceProvider.GetRequiredService<SmtpEmailService>(),
                    EmailProvider.None => new NullEmailService(),
                    _ => new NullEmailService()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating email service, falling back to null service");
                return new NullEmailService();
            }
        }
    }

    /// <summary>
    /// Null object pattern - does nothing when emails are disabled
    /// </summary>
    public class NullEmailService : IEmailService
    {
        public Task<bool> SendOrderConfirmationAsync(Order order) => Task.FromResult(false);
        public Task<bool> SendShippingNotificationAsync(Order order, string trackingNumber = "") => Task.FromResult(false);
        public Task<bool> SendPasswordResetAsync(string email, string resetLink) => Task.FromResult(false);
        public Task<bool> SendEmailVerificationAsync(ApplicationUser user, string verificationLink) => Task.FromResult(false);
        public Task<bool> SendWelcomeEmailAsync(ApplicationUser user) => Task.FromResult(false);
        public Task<bool> SendAdminOrderNotificationAsync(Order order) => Task.FromResult(false);
        public Task<bool> SendTestEmailAsync(string toEmail) => Task.FromResult(false);
        public Task<bool> IsConfiguredAsync() => Task.FromResult(false);
    }
}
