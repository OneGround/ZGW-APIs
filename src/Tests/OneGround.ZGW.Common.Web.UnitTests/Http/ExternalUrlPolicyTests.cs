using System.Net;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Common.Web.Http;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Http;

public class ExternalUrlPolicyTests
{
    private static ExternalJsonClientSettings Settings(params string[] allowedHosts) => new() { AllowedHosts = allowedHosts };

    [Theory]
    [InlineData("https://api.example.test/kanalen/1")]
    [InlineData("https://API.Example.Test/kanalen/1")]
    [InlineData("https://api.example.test:443/kanalen/1?x=1")]
    public void IsAllowedUrl_ExactHostOverHttps_IsAllowed(string url)
    {
        Assert.True(ExternalUrlPolicy.IsAllowedUrl(url, Settings("api.example.test"), out var uri));
        Assert.NotNull(uri);
    }

    [Theory]
    [InlineData("https://a.example.test/x", true)]
    [InlineData("https://a.b.example.test/x", true)]
    [InlineData("https://example.test/x", false)]
    [InlineData("https://notexample.test/x", false)]
    [InlineData("https://example.test.evil.test/x", false)]
    public void IsAllowedUrl_WildcardEntry_MatchesSubdomainsOnly(string url, bool expected)
    {
        Assert.Equal(expected, ExternalUrlPolicy.IsAllowedUrl(url, Settings("*.example.test"), out _));
    }

    [Theory]
    [InlineData("http://api.example.test/x")] // not https
    [InlineData("ftp://api.example.test/x")]
    [InlineData("https://other.example.test/x")] // not in allowlist
    [InlineData("https://user:pw@api.example.test/x")] // credentials
    [InlineData("https://api.example.test:8443/x")] // non-default port
    [InlineData("https://api.example.test:6379/x")]
    [InlineData("/relative/path")]
    [InlineData("niet-een-url")]
    [InlineData("")]
    public void IsAllowedUrl_Otherwise_IsRefused(string url)
    {
        Assert.False(ExternalUrlPolicy.IsAllowedUrl(url, Settings("api.example.test"), out var uri));
        Assert.Null(uri);
    }

    [Theory]
    [InlineData("https://api.example.test./x", "api.example.test")] // trailing dot in the url
    [InlineData("https://api.example.test/x", "api.example.test.")] // trailing dot in the entry
    [InlineData("https://bücher.example/x", "bücher.example")] // unicode host
    [InlineData("https://xn--bcher-kva.example/x", "bücher.example")] // punycode url, unicode entry
    [InlineData("https://bücher.example/x", "xn--bcher-kva.example")] // unicode url, punycode entry
    [InlineData("https://shop.bücher.example/x", "*.bücher.example")]
    [InlineData("https://API.Example.Test/x", " Api.Example.Test ")]
    public void IsAllowedUrl_EquivalentHostForms_AreAllowed(string url, string allowed)
    {
        Assert.True(ExternalUrlPolicy.IsAllowedUrl(url, Settings(allowed), out _));
    }

    [Fact]
    public void IsAllowedUrl_WildcardEntryWithCaseAndTrailingDot_IsAllowed()
    {
        Assert.True(ExternalUrlPolicy.IsAllowedUrl("https://shop.Example.Test/x", Settings("*.EXAMPLE.test."), out _));
    }

    [Theory]
    [InlineData("https://[::1]/x", "*.example.test")]
    [InlineData("https://[::1]/x", "example.test")]
    [InlineData("https://127.0.0.1/x", "*.example.test")]
    [InlineData("https://example.test/x", "ex ample.test")] // invalid entry never matches
    [InlineData("https://example.test/x", "-.test")]
    public void IsAllowedUrl_IpLiteralNotInAllowlistOrInvalidEntry_IsRefused(string url, string allowed)
    {
        Assert.False(ExternalUrlPolicy.IsAllowedUrl(url, Settings(allowed), out _));
    }

    [Theory]
    [InlineData("https://bücher.example/x", "buecher.example")]
    [InlineData("https://example.test/x", "*.example.test.")] // wildcard never matches the bare domain
    [InlineData("https://evil.test/x", "")]
    [InlineData("https://evil.test/x", "*.")]
    public void IsAllowedUrl_DifferentHost_IsRefused(string url, string allowed)
    {
        Assert.False(ExternalUrlPolicy.IsAllowedUrl(url, Settings(allowed), out _));
    }

    [Theory]
    [InlineData("api.example.test", true)]
    [InlineData("*.example.test", true)]
    [InlineData("bücher.example", true)]
    [InlineData("api.example.test.", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("*.", false)]
    [InlineData("ex ample.test", false)]
    [InlineData("-.test", false)]
    public void IsValidHostEntry_ReportsEntriesThatCanNeverMatch(string entry, bool expected)
    {
        Assert.Equal(expected, ExternalUrlPolicy.IsValidHostEntry(entry));
    }

    [Fact]
    public void IsAllowedUrl_EmptyAllowlist_RefusesEverything()
    {
        Assert.False(ExternalUrlPolicy.IsAllowedUrl("https://api.example.test/x", Settings(), out _));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.1.2.3")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")] // cloud metadata
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::10.0.0.1")] // IPv4-compatible
    [InlineData("::a9fe:a9fe")] // IPv4-compatible 169.254.169.254
    [InlineData("64:ff9b::a9fe:a9fe")] // NAT64 embedding 169.254.169.254
    [InlineData("64:ff9b::808:808")] // NAT64, refused outright
    [InlineData("64:ff9b:1::1")] // local-use NAT64
    [InlineData("64:ff9b:0:0:0:1:0:1")] // elsewhere in 64:ff9b::/32
    [InlineData("2002:a00:1::")] // 6to4 embedding 10.0.0.1
    public void IsPublicAddress_NonPublicAddress_IsFalse(string address)
    {
        Assert.False(ExternalUrlPolicy.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("172.15.0.1")]
    [InlineData("172.32.0.1")]
    [InlineData("100.63.0.1")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    public void IsPublicAddress_PublicAddress_IsTrue(string address)
    {
        Assert.True(ExternalUrlPolicy.IsPublicAddress(IPAddress.Parse(address)));
    }
}
