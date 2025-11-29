using EcommerceStarter.Services;

namespace EcommerceStarter.Middleware
{
    /// <summary>
    /// Middleware to add Google Tag Manager Gateway custom measurement path headers
    /// Required for Cloudflare Google Tag Gateway to work with auto-setup
    /// See: https://developers.google.com/tag-platform/tag-manager/gateway/setup-guide
    /// </summary>
    public class GoogleTagGatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GoogleTagGatewayMiddleware> _logger;

        public GoogleTagGatewayMiddleware(RequestDelegate next, ILogger<GoogleTagGatewayMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISiteSettingsService siteSettingsService)
        {
            try
            {
                // Get Google Tag ID (GTM or GA4) and measurement path from settings
                var settings = await siteSettingsService.GetSettingsAsync();
                var tagId = settings.GoogleAnalyticsMeasurementId;
                var measurementPath = settings.MeasurementPath ?? "/metrics";

                // Only add headers if a Google Tag is configured (GTM or GA4)
                if (!string.IsNullOrEmpty(tagId) && (tagId.StartsWith("GTM-") || tagId.StartsWith("G-")))
                {
                    // Use configured measurement path or default to /metrics
                    // Sanitize the path to remove any invalid characters (newlines, carriage returns, etc.)
                    var path = string.IsNullOrEmpty(measurementPath) ? "/metrics" : measurementPath.Trim();
                    
                    // Remove any control characters or non-ASCII characters that would cause header validation to fail
                    path = new string(path.Where(c => c >= 32 && c <= 126 && c != '\r' && c != '\n').ToArray());
                    
                    // Ensure path is not empty after sanitization
                    if (string.IsNullOrEmpty(path))
                    {
                        path = "/metrics";
                    }
                    
                    // Add the custom measurement path header
                    // This tells Google Tag Manager/Analytics to use Cloudflare's Gateway
                    context.Response.OnStarting(() =>
                    {
                        // Add Server-Timing header for Google Tag custom path
                        // Format: server-timing: gtm-server-path;desc="/metrics"
                        var headerValue = $"gtm-server-path;desc=\"{path}\"";
                        context.Response.Headers.Append("Server-Timing", headerValue);
                        
                        var tagType = tagId.StartsWith("GTM-") ? "GTM" : "GA4";
                        _logger.LogDebug("Added Google Tag Gateway header for {TagType} ID: {TagId}, Path: {Path}", tagType, tagId, path);
                        
                        return Task.CompletedTask;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding Google Tag Gateway headers");
                // Don't throw - this shouldn't break the request
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to add GoogleTagGatewayMiddleware to the pipeline
    /// </summary>
    public static class GoogleTagGatewayMiddlewareExtensions
    {
        public static IApplicationBuilder UseGoogleTagGateway(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GoogleTagGatewayMiddleware>();
        }
    }
}
