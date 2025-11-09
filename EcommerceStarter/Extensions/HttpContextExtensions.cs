namespace EcommerceStarter.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the client's IP address, handling proxies and load balancers
        /// </summary>
        public static string GetClientIpAddress(this HttpContext context)
        {
            // Try to get IP from X-Forwarded-For header (for reverse proxies like IIS, nginx)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs (client, proxy1, proxy2)
                // The first one is the original client
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    var ip = ips[0].Trim();
                    if (!string.IsNullOrEmpty(ip))
                        return ip;
                }
            }

            // Try to get from X-Real-IP header (used by some proxies)
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            // Try to get from CF-Connecting-IP (Cloudflare)
            var cloudflareIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(cloudflareIp))
                return cloudflareIp;

            // Fallback to RemoteIpAddress
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
                return remoteIp;

            return "Unknown";
        }

        /// <summary>
        /// Gets the user agent string from the request
        /// </summary>
        public static string GetUserAgent(this HttpContext context)
        {
            return context.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        }

        /// <summary>
        /// Gets the current request path
        /// </summary>
        public static string GetRequestPath(this HttpContext context)
        {
            return context.Request.Path.Value ?? "/";
        }
    }
}
