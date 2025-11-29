using System.Security.Claims;
using Microsoft.Extensions.Options;
using EcommerceStarter.Services;

namespace EcommerceStarter.Middleware
{
    /// <summary>
    /// Middleware to authenticate internal services (like automated testing) using a service key.
    /// This bypasses JWT authentication for internal system processes.
    /// NOTE: Feature disabled - InternalServiceKey moved to ApiConfigurations table
    /// </summary>
    public class InternalServiceAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InternalServiceAuthenticationMiddleware> _logger;
        private const string ServiceKeyHeader = "X-Internal-Service-Key";

        public InternalServiceAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<InternalServiceAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            ISiteSettingsService siteSettingsService,
            IEncryptionService encryptionService)
        {
            // Feature disabled - InternalServiceKey feature removed from SiteSettings
            // TODO: Implement using ApiConfigurations table if needed
            await _next(context);
        }
    }

    public static class InternalServiceAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseInternalServiceAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<InternalServiceAuthenticationMiddleware>();
        }
    }
}
