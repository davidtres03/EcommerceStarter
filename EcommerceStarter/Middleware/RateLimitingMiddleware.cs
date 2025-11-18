using System.Collections.Concurrent;
using EcommerceStarter.Extensions;
using EcommerceStarter.Services;
using Microsoft.AspNetCore.Identity;
using EcommerceStarter.Models;

namespace EcommerceStarter.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, RequestCounter> _requestCounters = new();

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            ISecuritySettingsService securitySettingsService,
            UserManager<ApplicationUser> userManager)
        {
            var settings = await securitySettingsService.GetSettingsAsync();

            // Check if rate limiting is enabled
            if (!settings.EnableRateLimiting)
            {
                await _next(context);
                return;
            }

            var ipAddress = context.GetClientIpAddress();

            // Check if IP is whitelisted (whitelisted IPs bypass all rate limiting)
            if (await securitySettingsService.IsIpWhitelistedAsync(ipAddress))
            {
                _logger.LogDebug("Whitelisted IP {IpAddress} bypassing rate limiting", ipAddress);
                await _next(context);
                return;
            }

            // Determine user authentication status and role
            bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;
            bool isAuthenticatedAdmin = false;
            bool isAuthenticatedCustomer = false;
            string userIdentifier = ipAddress; // Default to IP for anonymous users

            if (isAuthenticated)
            {
                isAuthenticatedAdmin = context.User.IsInRole("Admin");
                isAuthenticatedCustomer = context.User.IsInRole("Customer");
                
                // Use email/username as identifier for authenticated users (more specific than IP)
                userIdentifier = context.User.Identity?.Name ?? ipAddress;
                
                // Check if admin and admins are exempt
                if (settings.ExemptAdminsFromRateLimiting && isAuthenticatedAdmin)
                {
                    var userName = context.User.Identity?.Name ?? "Unknown";
                    _logger.LogInformation("Admin user {UserName} (IP: {IpAddress}) bypassing rate limiting on {Path}", 
                        userName, 
                        ipAddress,
                        context.Request.Path);
                    
                    await _next(context);
                    return;
                }
            }

            var endpoint = context.Request.Path.Value?.ToLower() ?? "";

            // Determine rate limits based on endpoint and user type
            // Priority: Auth endpoints get strictest limits, then regular authenticated users, then anonymous
            int maxRequests;
            int maxRequestsPerSec;

            // Auth endpoints (login, register, admin) get stricter limits
            bool isAuthEndpoint = endpoint.Contains("/account/login") || 
                                  endpoint.Contains("/account/register") ||
                                  endpoint.Contains("/admin");

            if (isAuthEndpoint)
            {
                // Strictest limits for sensitive endpoints
                maxRequests = settings.MaxRequestsPerMinuteAuth;
                maxRequestsPerSec = settings.MaxRequestsPerSecondAuth;
            }
            else if (isAuthenticatedCustomer)
            {
                // Authenticated customers get better limits than anonymous users
                // Use the general limits (which are typically higher than auth endpoint limits)
                maxRequests = settings.MaxRequestsPerMinute;
                maxRequestsPerSec = settings.MaxRequestsPerSecond;
            }
            else
            {
                // Anonymous/guest users and non-exempt admins get standard limits
                maxRequests = settings.MaxRequestsPerMinute;
                maxRequestsPerSec = settings.MaxRequestsPerSecond;
            }

            // Use userIdentifier for rate limiting (email for authenticated, IP for anonymous)
            // This provides better granularity: authenticated users tracked separately even if sharing IP
            var counter = _requestCounters.GetOrAdd(userIdentifier, _ => new RequestCounter());

            // Check per-second rate limit
            if (counter.GetRequestsInLastSecond() >= maxRequestsPerSec)
            {
                _logger.LogWarning("Rate limit exceeded (per second) | Identifier: {Identifier} | IP: {IpAddress} | Path: {Path} | User: {User} | Authenticated: {IsAuth} | Admin: {IsAdmin} | Customer: {IsCustomer} | Requests/sec: {RequestCount}/{Limit}", 
                    userIdentifier,
                    ipAddress, 
                    endpoint, 
                    context.User.Identity?.Name ?? "Anonymous",
                    isAuthenticated, 
                    isAuthenticatedAdmin,
                    isAuthenticatedCustomer,
                    counter.GetRequestsInLastSecond(),
                    maxRequestsPerSec);
                    
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Append("Retry-After", "1");
                context.Response.Headers.Append("X-RateLimit-Limit", maxRequestsPerSec.ToString());
                context.Response.Headers.Append("X-RateLimit-Remaining", "0");
                await context.Response.WriteAsync("Rate limit exceeded. Please slow down your requests.");
                return;
            }

            // Check per-minute rate limit
            if (counter.GetRequestsInLastMinute() >= maxRequests)
            {
                _logger.LogWarning("Rate limit exceeded (per minute) | Identifier: {Identifier} | IP: {IpAddress} | Path: {Path} | User: {User} | Authenticated: {IsAuth} | Admin: {IsAdmin} | Customer: {IsCustomer} | Requests/min: {RequestCount}/{Limit}", 
                    userIdentifier,
                    ipAddress, 
                    endpoint,
                    context.User.Identity?.Name ?? "Anonymous",
                    isAuthenticated, 
                    isAuthenticatedAdmin,
                    isAuthenticatedCustomer,
                    counter.GetRequestsInLastMinute(),
                    maxRequests);
                    
                context.Response.StatusCode = 429;
                context.Response.Headers.Append("Retry-After", "60");
                context.Response.Headers.Append("X-RateLimit-Limit", maxRequests.ToString());
                context.Response.Headers.Append("X-RateLimit-Remaining", "0");
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Add rate limit headers to successful responses
            var remaining = Math.Max(0, maxRequests - counter.GetRequestsInLastMinute());
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append("X-RateLimit-Limit", maxRequests.ToString());
                context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString());
                context.Response.Headers.Append("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString());
                return Task.CompletedTask;
            });

            counter.AddRequest();
            await _next(context);
        }

        private class RequestCounter
        {
            private readonly Queue<DateTime> _requestTimes = new();
            private readonly object _lock = new();

            public void AddRequest()
            {
                lock (_lock)
                {
                    _requestTimes.Enqueue(DateTime.UtcNow);
                    CleanOldRequests();
                }
            }

            public int GetRequestsInLastMinute()
            {
                lock (_lock)
                {
                    CleanOldRequests();
                    return _requestTimes.Count(r => r > DateTime.UtcNow.AddMinutes(-1));
                }
            }

            public int GetRequestsInLastSecond()
            {
                lock (_lock)
                {
                    CleanOldRequests();
                    return _requestTimes.Count(r => r > DateTime.UtcNow.AddSeconds(-1));
                }
            }

            private void CleanOldRequests()
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-2);
                while (_requestTimes.Count > 0 && _requestTimes.Peek() < cutoff)
                {
                    _requestTimes.Dequeue();
                }
            }
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
