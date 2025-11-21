using EcommerceStarter.Converters;
using EcommerceStarter.Services;

namespace EcommerceStarter.Middleware
{
    /// <summary>
    /// Middleware that sets the ITimezoneService in the async context for use by JSON converters.
    /// This allows the global DateTime JSON converter to access the scoped timezone service
    /// during request processing.
    /// </summary>
    public class TimezoneServiceMiddleware
    {
        private readonly RequestDelegate _next;

        public TimezoneServiceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITimezoneService timezoneService)
        {
            // Set the timezone service in the async local context
            // This makes it available to the JSON converters during serialization
            TimezoneServiceAccessor.TimezoneService = timezoneService;

            try
            {
                await _next(context);
            }
            finally
            {
                // Clean up after request
                TimezoneServiceAccessor.TimezoneService = null;
            }
        }
    }

    /// <summary>
    /// Extension method for easy middleware registration
    /// </summary>
    public static class TimezoneServiceMiddlewareExtensions
    {
        public static IApplicationBuilder UseTimezoneService(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TimezoneServiceMiddleware>();
        }
    }
}
