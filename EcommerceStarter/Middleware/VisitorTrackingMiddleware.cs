using EcommerceStarter.Services.Analytics;
using EcommerceStarter.Services;

namespace EcommerceStarter.Middleware
{
    /// <summary>
    /// Middleware to automatically track page views and visitor sessions
    /// </summary>
    public class VisitorTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<VisitorTrackingMiddleware> _logger;

        public VisitorTrackingMiddleware(
            RequestDelegate next,
            ILogger<VisitorTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IVisitorTrackingService trackingService, IQueuedEventService queuedEventService)
        {
            try
            {
                // Skip tracking for static files, API calls, and admin panel
                var path = context.Request.Path.Value?.ToLower() ?? "";
                
                if (ShouldTrack(path))
                {
                    // Get or create session (still synchronous - needed for session ID)
                    var session = await trackingService.GetOrCreateSessionAsync(context);

                    // CRITICAL: Don't track whitelisted/internal IPs (they have temporary sessions with Id = 0)
                    // Whitelisted sessions have SessionId starting with "whitelist-" and Id = 0
                    if (session.Id > 0)
                    {
                        // Queue page view (non-blocking) instead of direct DB write
                        var url = context.Request.Path + context.Request.QueryString;
                        var referrer = context.Request.Headers["Referer"].ToString();
                        var pageTitle = ExtractPageTitle(path);

                        queuedEventService.QueuePageView(
                            session.Id,
                            url,
                            pageTitle,
                            referrer
                        );

                        // Store session ID in context for use in other services
                        context.Items["VisitorSessionId"] = session.Id;
                    }
                    else
                    {
                        _logger.LogDebug("Skipping page view tracking for whitelisted/internal IP session");
                    }
                }
            }
            catch (Exception ex)
            {
                // Don't let tracking errors break the request
                _logger.LogError(ex, "Error in visitor tracking middleware");
            }

            await _next(context);
        }

        /// <summary>
        /// Determine if the request should be tracked
        /// </summary>
        private bool ShouldTrack(string path)
        {
            // Don't track static files
            if (path.Contains(".css") || path.Contains(".js") || 
                path.Contains(".jpg") || path.Contains(".jpeg") ||
                path.Contains(".png") || path.Contains(".gif") ||
                path.Contains(".svg") || path.Contains(".ico") ||
                path.Contains(".woff") || path.Contains(".woff2") ||
                path.Contains(".ttf") || path.Contains(".eot"))
            {
                return false;
            }

            // Don't track API endpoints
            if (path.StartsWith("/api/"))
            {
                return false;
            }

            // Don't track health checks
            if (path.StartsWith("/health"))
            {
                return false;
            }

            // Don't track admin panel
            if (path.StartsWith("/admin/"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extract a friendly page title from the path
        /// </summary>
        private string? ExtractPageTitle(string path)
        {
            if (path == "/" || path == "/index")
                return "Home";
            
            if (path.StartsWith("/products/details"))
                return "Product Details";
            
            if (path.StartsWith("/products"))
                return "Products";
            
            if (path.StartsWith("/cart"))
                return "Shopping Cart";
            
            if (path.StartsWith("/checkout"))
                return "Checkout";
            
            if (path.StartsWith("/orders/confirmation"))
                return "Order Confirmation";
            
            if (path.StartsWith("/orders"))
                return "My Orders";
            
            if (path.StartsWith("/account/login"))
                return "Login";
            
            if (path.StartsWith("/account/register"))
                return "Register";
            
            if (path.StartsWith("/admin"))
                return "Admin Panel";

            return null;
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
    public static class VisitorTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UseVisitorTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<VisitorTrackingMiddleware>();
        }
    }
}
