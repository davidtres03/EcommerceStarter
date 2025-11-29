using System.Net;

namespace EcommerceStarter.Utilities
{
    public static class CIDRHelper
    {
        public static bool IsInCIDRRange(string ipAddress, string entry)
        {
            if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(entry))
                return false;

            // Simple direct IP match
            if (!entry.Contains('/'))
                return string.Equals(ipAddress.Trim(), entry.Trim(), StringComparison.OrdinalIgnoreCase);

            // Basic IPv4 CIDR support: a.b.c.d/prefix
            var parts = entry.Split('/');
            if (parts.Length != 2)
                return false;

            if (!IPAddress.TryParse(parts[0], out var networkIp))
                return false;
            if (!int.TryParse(parts[1], out var prefixLength))
                return false;

            if (networkIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false; // Only IPv4 supported in this helper

            if (!IPAddress.TryParse(ipAddress, out var addr))
                return false;
            if (addr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            uint ip = BitConverter.ToUInt32(addr.GetAddressBytes().Reverse().ToArray(), 0);
            uint net = BitConverter.ToUInt32(networkIp.GetAddressBytes().Reverse().ToArray(), 0);

            if (prefixLength < 0 || prefixLength > 32)
                return false;

            uint mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
            return (ip & mask) == (net & mask);
        }
    }
}
