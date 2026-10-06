using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using OneGround.ZGW.Common.Web.Configuration;

namespace OneGround.ZGW.Common.Web.Http;

/// <summary>
/// Decides whether an untrusted url / resolved address may be called by <see cref="IExternalJsonClient"/>.
/// </summary>
public static class ExternalUrlPolicy
{
    /// <summary>
    /// Only absolute https urls on the default port (443) without credentials, to a host that is in
    /// <see cref="ExternalJsonClientSettings.AllowedHosts"/>. The port is fixed so an allowed host cannot be used to probe or reach
    /// other services (databases, caches, admin ports) running on it.
    /// </summary>
    public static bool IsAllowedUrl(string url, ExternalJsonClientSettings settings, out Uri uri)
    {
        uri = null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        if (parsed.Scheme != Uri.UriSchemeHttps || parsed.Port != 443 || !string.IsNullOrEmpty(parsed.UserInfo))
            return false;

        if (!settings.AllowedHosts.Any(allowed => HostMatches(parsed.IdnHost, allowed)))
            return false;

        uri = parsed;
        return true;
    }

    private static bool HostMatches(string host, string allowed)
    {
        if (string.IsNullOrWhiteSpace(allowed))
            return false;

        allowed = allowed.Trim();

        if (allowed.StartsWith("*.", StringComparison.Ordinal))
            return host.EndsWith(allowed[1..], StringComparison.OrdinalIgnoreCase) && host.Length > allowed.Length - 1;

        return string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True only for addresses on the public internet. Everything that can reach an internal service is refused: loopback,
    /// link-local (incl. cloud metadata 169.254.169.254), private (RFC 1918), shared/CGNAT, unique-local IPv6, multicast,
    /// unspecified and reserved ranges. IPv4-mapped IPv6 addresses are judged by their IPv4 address.
    /// </summary>
    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.None))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Forms that embed an IPv4 address (and so may reach a private one on a network that translates them) are refused outright:
            // IPv4-compatible (::/96), NAT64 (64:ff9b::/96 and the local-use 64:ff9b:1::/48) and 6to4 (2002::/16).
            var v6 = address.GetAddressBytes();
            var isIpv4Compatible = Array.TrueForAll(v6[..12], x => x == 0);
            var isNat64 = v6[0] == 0x00 && v6[1] == 0x64 && v6[2] == 0xff && v6[3] == 0x9b && (v6[4] == 0 || v6[4] == 1);
            var is6To4 = v6[0] == 0x20 && v6[1] == 0x02;

            if (isIpv4Compatible || isNat64 || is6To4)
                return false;

            return !(
                address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || address.IsIPv6UniqueLocal || address.IsIPv6Teredo
            );
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var b = address.GetAddressBytes();

        return !(
            b[0] == 0 // 0.0.0.0/8
            || b[0] == 10 // 10.0.0.0/8
            || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) // 100.64.0.0/10 shared address space
            || (b[0] == 169 && b[1] == 254) // 169.254.0.0/16 link-local
            || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) // 172.16.0.0/12
            || (b[0] == 192 && b[1] == 0 && b[2] == 0) // 192.0.0.0/24 IETF protocol assignments
            || (b[0] == 192 && b[1] == 168) // 192.168.0.0/16
            || (b[0] == 198 && (b[1] == 18 || b[1] == 19)) // 198.18.0.0/15 benchmarking
            || b[0] >= 224 // multicast + reserved + broadcast
        );
    }
}
