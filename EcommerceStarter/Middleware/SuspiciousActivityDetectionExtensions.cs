using Microsoft.AspNetCore.Builder;

namespace EcommerceStarter.Middleware
{
    public static class SuspiciousActivityDetectionExtensions
    {
        public static IApplicationBuilder UseSuspiciousActivityDetection(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SuspiciousActivityDetectionMiddleware>();
        }
    }
}
