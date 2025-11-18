using EcommerceStarter.Services.Analytics;

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

        public async Task InvokeAsync(HttpContext context, IVisitorTrackingService trackingService)
        {
            try
            {
                // Skip tracking for static files, API calls, and admin panel
                var path = context.Request.Path.Value?.ToLower() ?? "";
                
                if (ShouldTrack(path))
                {
                    // Get or create session
                    var session = await trackingService.GetOrCreateSessionAsync(context);

                    // Track page view
                    var url = context.Request.Path + context.Request.QueryString;
                    var referrer = context.Request.Headers["Referer"].ToString();
                    var pageTitle = ExtractPageTitle(path);

                    await trackingService.TrackPageViewAsync(
                        session.Id,
                        url,
                        pageTitle,
                        referrer
                    );

                    // Store session ID in context for use in other services
                    context.Items["VisitorSessionId"] = session.Id;
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
