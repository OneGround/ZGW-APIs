using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Extensions;

/// <summary>
/// The obsolete Microsoft.AspNetCore.HttpOverrides.IPNetwork accepted a KnownNetworks entry with host bits set
/// (e.g. "10.0.0.1/8"). These tests guard that such existing configuration keeps working with System.Net.IPNetwork,
/// which normalizes the base address to the prefix instead of failing at startup.
/// </summary>
public class ForwardedHeadersServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("10.0.0.0/8", "10.0.0.0/8")]
    [InlineData("10.0.0.1/8", "10.0.0.0/8")]
    [InlineData("192.168.1.77/24", "192.168.1.0/24")]
    [InlineData("fd00::1/64", "fd00::/64")]
    public void ConfigureForwardedHeaders_accepts_known_networks_and_normalizes_host_bits(string configured, string expected)
    {
        var options = ResolveOptions(configured);

        var network = Assert.Single(options.KnownIPNetworks);
        Assert.Equal(expected, network.ToString());
    }

    [Fact]
    public void ConfigureForwardedHeaders_known_network_with_host_bits_matches_addresses_in_the_prefix()
    {
        var options = ResolveOptions("10.0.0.1/8");

        var network = Assert.Single(options.KnownIPNetworks);
        Assert.True(network.Contains(IPAddress.Parse("10.9.9.9")));
        Assert.False(network.Contains(IPAddress.Parse("11.0.0.1")));
    }

    private static ForwardedHeadersOptions ResolveOptions(params string[] knownNetworks)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(knownNetworks.Select((n, i) => new KeyValuePair<string, string>($"ForwardedHeaders:KnownNetworks:{i}", n)))
            .Build();

        var services = new ServiceCollection();
        services.ConfigureForwardedHeaders(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
    }
}
