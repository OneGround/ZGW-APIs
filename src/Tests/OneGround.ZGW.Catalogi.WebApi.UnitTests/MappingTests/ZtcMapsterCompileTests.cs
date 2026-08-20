using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class ZtcMapsterCompileTests
{
    /// <summary>
    /// Compiles every registered type pair up front, so a register that makes Mapster emit an
    /// endlessly-recursive mapping function fails here instead of at runtime. Failure is an aborted host
    /// or a hung run, never a failed assertion — treat either as real, not as flakiness.
    /// </summary>
    [Fact]
    public void AddZgwMapster_config_compiles_every_registered_type_pair()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // Compile() over an empty RuleMap passes, so without this a broken config.Scan would turn the
        // gate green rather than red.
        Assert.NotEmpty(config.RuleMap);

        config.Compile();
    }

    /// <summary>
    /// Mapster's stand-in for AutoMapper's <c>AssertConfigurationIsValid()</c>: every destination member
    /// needs a source member, an explicit <c>.Map(...)</c> or an explicit <c>.Ignore(...)</c>. This is
    /// what keeps the registers' <c>.Ignore(...)</c> calls load-bearing rather than decorative.
    /// </summary>
    [Fact]
    public void Every_registered_type_pair_maps_or_ignores_every_destination_member()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // On the test's own config, never inside AddZgwMapster: as a global seam setting this would throw
        // at startup for every service that has not migrated and has no registers at all.
        config.Default.RequireDestinationMemberSource(true);

        var unmapped = new List<string>();

        // Per pair rather than one config.Compile(), which throws on the first failure and would make a
        // multi-member regression take several rounds to clear.
        foreach (var pair in config.RuleMap.Keys.OrderBy(k => k.Source.FullName).ThenBy(k => k.Destination.FullName).ToList())
        {
            try
            {
                config.Compile(pair.Source, pair.Destination);
            }
            catch (Exception ex)
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
