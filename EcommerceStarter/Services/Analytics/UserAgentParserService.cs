using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using UAParser;

namespace EcommerceStarter.Services.Analytics
{
    public interface IUserAgentParserService
    {
        UserAgentInfo Parse(string userAgent, IHeaderDictionary headers);
    }

    public class UserAgentInfo
    {
        public string? Browser { get; set; }
        public string? BrowserVersion { get; set; }
        public string? OperatingSystem { get; set; }
        public string? OSVersion { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceBrand { get; set; }
        public string? DeviceModel { get; set; }
        public bool IsBot { get; set; }
        public string? BotName { get; set; }
    }

    public class UserAgentParserService : IUserAgentParserService
    {
        private readonly Parser _parser;
        private readonly ILogger<UserAgentParserService> _logger;

        public UserAgentParserService(ILogger<UserAgentParserService> logger)
        {
            _parser = Parser.GetDefault();
            _logger = logger;
        }

        public UserAgentInfo Parse(string userAgent, IHeaderDictionary headers)
        {
            var info = new UserAgentInfo();
            try
            {
                var client = _parser.Parse(userAgent ?? string.Empty);

                info.Browser = client.UA?.Family;
                info.BrowserVersion = BuildVersion(client.UA);
                info.OperatingSystem = client.OS?.Family;
                info.OSVersion = BuildVersion(client.OS);
                info.DeviceBrand = client.Device?.Brand;
                info.DeviceModel = client.Device?.Model;
                info.DeviceType = InferDeviceType(client);

                var (isBot, botName) = DetectBot(userAgent, headers, client);
                info.IsBot = isBot;
                info.BotName = botName;
            }
            catch (System.Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse user agent");
                info.Browser = "Unknown";
                info.OperatingSystem = "Unknown";
                info.DeviceType = "Unknown";
            }

            return info;
        }

        private static string? BuildVersion(UserAgent? ua)
        {
            if (ua == null) return null;
            var parts = new[] { ua.Major, ua.Minor, ua.Patch }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var v = string.Join('.', parts);
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        private static string? BuildVersion(OS? os)
        {
            if (os == null) return null;
            var parts = new[] { os.Major, os.Minor, os.Patch, os.PatchMinor }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            var v = string.Join('.', parts);
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        private static string InferDeviceType(ClientInfo c)
        {
            var family = c.Device?.Family ?? string.Empty;
            var f = family.ToLowerInvariant();

            // Bot detection first
            if (f.Contains("spider") || f.Contains("bot") || f.Contains("crawler"))
                return "Bot";

            // Mobile detection
            if (f.Contains("iphone") || f.Contains("ipod") || f.Contains("blackberry") || 
                f.Contains("windows phone") || f.Contains("nokia") || 
                (f.Contains("android") && !f.Contains("tablet")) || 
                f.Contains("mobile"))
                return "Mobile";

            // Tablet detection
            if (f.Contains("ipad") || f.Contains("tablet") || f.Contains("kindle"))
                return "Tablet";

            // OS-based detection for desktop
            var osfam = c.OS?.Family?.ToLowerInvariant() ?? string.Empty;
            if (osfam.Contains("windows") || osfam.Contains("mac os") || osfam.Contains("macos") ||
                osfam.Contains("linux") || osfam.Contains("ubuntu") || osfam.Contains("debian") ||
                osfam.Contains("chrome os") || osfam.Contains("fedora"))
                return "Desktop";

            // Browser family hints
            var browser = c.UA?.Family?.ToLowerInvariant() ?? string.Empty;
            if (browser.Contains("chrome") || browser.Contains("firefox") || 
                browser.Contains("safari") || browser.Contains("edge") || browser.Contains("opera"))
                return "Desktop";

            return "Unknown";
        }

        private static (bool isBot, string? botName) DetectBot(string ua, IHeaderDictionary headers, ClientInfo client)
        {
            var uaStr = ua ?? string.Empty;
            var lower = uaStr.ToLowerInvariant();

            string[] botKeywords = new[]
            {
                "bot","spider","crawler","crawl","slurp","bingpreview",
                "mediapartners-google","facebookexternalhit","linkedinbot","duckduckbot",
                "baiduspider","yandex","semrushbot","ahrefsbot","mj12bot","dotbot",
                "rogerbot","exabot","facebot","ia_archiver","archive.org","wget","curl",
                "python-requests","go-http-client","java/","http_request2","applebot",
                "googlebot","bingbot","yahoo! slurp","duckduckbot","baiduspider","sogou",
                "teoma","alexibot","msnbot","icc-crawler","mojeekbot","seznambot"
            };
            
            foreach (var kw in botKeywords)
            {
                if (lower.Contains(kw))
                {
                    // Extract more specific bot name if possible
                    var parts = lower.Split(new[] { ' ', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    var botPart = parts.FirstOrDefault(p => p.Contains("bot") || p.Contains("crawler") || p.Contains("spider"));
                    return (true, botPart ?? kw);
                }
            }

            // Heuristic: missing common headers may indicate bot
            if (string.IsNullOrWhiteSpace(headers["Accept-Language"]) && lower.Contains("http"))
                return (true, "UnknownBot");

            // UAParser sometimes labels device family as Spider
            var family = client.Device?.Family?.ToLowerInvariant() ?? string.Empty;
            if (family.Contains("spider") || family.Contains("bot"))
                return (true, family);

            return (false, null);
        }
    }
}
