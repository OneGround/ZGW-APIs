using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Documenten.Web;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

public class DrcMapsterCompileTests
{
    // Failure here is a hung run or an aborted host, not a failed assertion -- treat either as real, not as flakiness.
    [Fact]
    public void AddZgwMapster_config_compiles_every_registered_type_pair()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // An empty RuleMap would still Compile() cleanly -- this guard makes a broken config.Scan fail loudly instead.
        Assert.NotEmpty(config.RuleMap);

        config.Compile();
    }

    /// <summary>
    /// Mapster's analogue of AutoMapper's <c>AssertConfigurationIsValid()</c>: every destination member needs a
    /// source, a <c>.Map(...)</c> or an <c>.Ignore(...)</c>. Only covers pairs with an explicit <c>NewConfig</c>
    /// entry -- a pair mapped purely by Mapster's bare convention has no <c>RuleMap</c> entry and is invisible here.
    /// </summary>
    [Fact]
    public void Every_registered_type_pair_maps_or_ignores_every_destination_member()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // On the test's own config, never inside AddZgwMapster: as a global seam setting it would also apply
        // to pairs with no register entry, which compile lazily on their first Map() call rather than at
        // startup -- so the failure would surface on a live request instead of here.
        config.Default.RequireDestinationMemberSource(true);

        var unmapped = new List<string>();

        // Compiled per pair rather than via one config.Compile(), which throws on the first failure and would
        // make a multi-member regression take several rounds to clear.
        foreach (var pair in config.RuleMap.Keys.OrderBy(k => k.Source.FullName).ThenBy(k => k.Destination.FullName).ToList())
        {
            try
            {
                config.Compile(pair.Source, pair.Destination);
            }
            catch (CompileException ex)
            {
                // Mapster puts the member names in the inner exception; the outer one only repeats the pair.
                unmapped.Add($"{pair.Source.FullName} -> {pair.Destination.FullName}\n    {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        Assert.True(
            unmapped.Count == 0,
            "These destination members have no source, no .Map(...) and no .Ignore(...). Map them, or "
                + "add an explicit .Ignore(...) recording that leaving them at their default is intended:\n  "
                + string.Join("\n  ", unmapped)
        );
    }
}
