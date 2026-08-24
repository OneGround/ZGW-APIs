using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Zaken.Web;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class ZrcMapsterCompileTests
{
    /// <summary>
    /// The polymorphic base pairs, excluded from the completeness gate below. Their destinations carry one
    /// navigation per concrete subtype, and the base request DTO has no source member for any of them — so
    /// the only annotation the gate would accept is an <c>.Ignore(...)</c> on the BASE config. That is
    /// exactly what must not be added: a base-config rule for a member wins over every derived config's
    /// rule for it, so ignoring the subtype navigations here silently blanks the identification object on
    /// every write through every subtype. Measured on these registers: mapping an address case object
    /// yields the identification with the base ignores absent, and null with them present.
    /// <para>
    /// The inverse fact —
    /// <see cref="ZrcPolymorphicBaseConfigTests.The_polymorphic_base_configs_declare_no_rule_for_a_subtype_navigation"/>
    /// — asserts these base configs declare no rule for a subtype navigation. That is what makes this
    /// exclusion safe rather than a hole; the two halves only work as a pair.
    /// </para>
    /// </summary>
    private static readonly string[] PolymorphicBasePairs =
    [
        "OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.ZaakObjectRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakObject.ZaakObject",
        "OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakObject.ZaakObject",
        "OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol.ZaakRolRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol",
        "OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol.ZaakRolRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol",
    ];

    /// <summary>
    /// Compiles every registered type pair up front, so a register that makes Mapster emit an
    /// endlessly-recursive mapping function fails here instead of killing the process at runtime with no
    /// log entry. Failure is an aborted host or a hung run, never a failed assertion — treat either as
    /// real, not as flakiness.
    /// </summary>
    /// <remarks>
    /// Measured green on this service's registers: every path that reaches a type with a navigation back
    /// to one of its own ancestors is already ignored, so the generator's walk terminates. This fact
    /// exists to keep that true as registers change.
    /// </remarks>
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
    /// Mapster's stand-in for AutoMapper's <c>AssertConfigurationIsValid()</c>, which this service lost
    /// when its profile tests were retargeted: every destination member needs a source member, an explicit
    /// <c>.Map(...)</c> or an explicit <c>.Ignore(...)</c>. This is what keeps the registers'
    /// <c>.Ignore(...)</c> calls load-bearing rather than decorative.
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
        var skipped = 0;

        // Per pair rather than one config.Compile(), which throws on the first failure and would make a
        // multi-member regression take several rounds to clear.
        foreach (var pair in config.RuleMap.Keys.OrderBy(k => k.Source.FullName).ThenBy(k => k.Destination.FullName).ToList())
        {
            var name = $"{pair.Source.FullName} -> {pair.Destination.FullName}";
            if (PolymorphicBasePairs.Contains(name))
            {
                skipped++;
                continue;
            }

            try
            {
                config.Compile(pair.Source, pair.Destination);
            }
            catch (CompileException ex)
            {
                // Mapster puts the member names in the inner exception; the outer one only repeats the pair.
                unmapped.Add($"{name}\n    {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // A skip-list entry that matches no real pair is a dead string: the base pair it was meant to
        // exclude stays in the gate, and the obvious way to make the gate pass is the base-config
        // .Ignore(...) that blanks the identification object on write. Assert the exclusion actually fired.
        Assert.Equal(PolymorphicBasePairs.Length, skipped);

        Assert.True(
            unmapped.Count == 0,
            "These destination members have no source, no .Map(...) and no .Ignore(...). Map them, or "
                + "add an explicit .Ignore(...) recording that leaving them at their default is intended:\n  "
                + string.Join("\n  ", unmapped)
        );
    }
}
