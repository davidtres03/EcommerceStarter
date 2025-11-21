using System.Security.Claims;
using Microsoft.Extensions.Options;
using EcommerceStarter.Services;

namespace EcommerceStarter.Middleware
{
    /// <summary>
    /// Middleware to authenticate internal services (like automated testing) using a service key.
    /// This bypasses JWT authentication for internal system processes.
    /// Service key is stored encrypted in SiteSettings database table.
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
            // Check if request contains internal service key
            if (context.Request.Headers.TryGetValue(ServiceKeyHeader, out var headerValue))
            {
                var providedKey = headerValue.ToString();
                
                // Retrieve encrypted service key from database
                var settings = await siteSettingsService.GetSettingsAsync();
                if (string.IsNullOrEmpty(settings.InternalServiceKeyEncrypted))
                {
                    _logger.LogWarning("Internal service key not configured in database");
                    context.Response.StatusCode = 503;
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        error = "Internal service authentication not configured",
                        timestamp = DateTime.UtcNow 
                    });
                    return;
                }
                
                var serviceKey = encryptionService.Decrypt(settings.InternalServiceKeyEncrypted);

                if (!string.IsNullOrWhiteSpace(providedKey) && providedKey == serviceKey)
                {
                    // Valid internal service key - create authenticated identity
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, "InternalService"),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim("ServiceType", "ApiTestRunner"),
                        new Claim("AuthMethod", "InternalServiceKey")
                    };

                    var identity = new ClaimsIdentity(claims, "InternalService");
                    context.User = new ClaimsPrincipal(identity);

                    _logger.LogInformation("Internal service authenticated for path: {Path}", context.Request.Path);
                }
                else
                {
                    _logger.LogWarning("Invalid internal service key provided from IP: {IP}", 
                        context.Connection.RemoteIpAddress);
                    
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        error = "Invalid internal service key",
                        timestamp = DateTime.UtcNow 
                    });
                    return;
                }
            }

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
