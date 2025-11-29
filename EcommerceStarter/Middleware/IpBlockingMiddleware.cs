using EcommerceStarter.Services;
using EcommerceStarter.Extensions;
using System.Net;

namespace EcommerceStarter.Middleware
{
    public class IpBlockingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpBlockingMiddleware> _logger;

        public IpBlockingMiddleware(RequestDelegate next, ILogger<IpBlockingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            ISecurityAuditService securityAuditService,
            ISecuritySettingsService securitySettingsService)
        {
            var settings = await securitySettingsService.GetSettingsAsync();

            // Check if IP blocking is enabled
            if (!settings.EnableIpBlocking)
            {
                await _next(context);
                return;
            }

            var ipAddress = context.GetClientIpAddress();

            // Check if IP is whitelisted (whitelisted IPs bypass all blocking)
            if (await securitySettingsService.IsIpWhitelistedAsync(ipAddress))
            {
                await _next(context);
                return;
            }

            // Check if IP is permanently blacklisted
            if (await securitySettingsService.IsIpBlacklistedAsync(ipAddress))
            {
                _logger.LogWarning("Blacklisted IP attempted access: {IpAddress} to {Path}", ipAddress, context.Request.Path);
                
                await securityAuditService.LogSecurityEventAsync(
                    "BlacklistedIpAttempt", 
                    "Critical", 
                    ipAddress,
                    details: $"Blacklisted IP attempted to access: {context.Request.Path}",
                    endpoint: context.GetRequestPath(),
                    userAgent: context.GetUserAgent(),
                    isBlocked: true);

                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Access denied. Your IP address is permanently blocked.");
                return;
            }

            // Check if IP is temporarily blocked
            if (await securityAuditService.IsIpBlockedAsync(ipAddress))
            {
                _logger.LogWarning("Blocked IP attempted access: {IpAddress} to {Path}", ipAddress, context.Request.Path);
                
                await securityAuditService.LogSecurityEventAsync(
                    "BlockedIpAttempt", 
                    "High", 
                    ipAddress,
                    details: $"Blocked IP attempted to access: {context.Request.Path}",
                    endpoint: context.GetRequestPath(),
                    userAgent: context.GetUserAgent(),
                    isBlocked: true);

                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Access denied. Your IP address has been blocked due to suspicious activity.");
                return;
            }

            await _next(context);
        }
    }

    public static class IpBlockingMiddlewareExtensions
    {
        public static IApplicationBuilder UseIpBlocking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpBlockingMiddleware>();
        }
    }
}
