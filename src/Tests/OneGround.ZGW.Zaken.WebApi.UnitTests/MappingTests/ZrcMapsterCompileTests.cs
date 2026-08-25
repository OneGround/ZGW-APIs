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
    /// <para>
    /// Observed failure mode, so nobody has to re-derive it: adding an entity-to-entity pair
    /// (<c>config.NewConfig&lt;ZaakObject, ZaakObject&gt;()</c>) to a register makes the run HANG. There is
    /// no assertion message and no output past the test-discovery line; it was still running after seven
    /// minutes against a clean run of about one second, and the host went down only when the run was killed.
    /// The recursion is in Mapster's code generator while it BUILDS the expression tree, so nothing reaches
    /// the point of throwing. A hang or an aborted host IS this gate firing — never read it as flakiness,
    /// and never conclude the gate is unverified because no red assertion appeared.
    /// </para>
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
    /// The only whole-configuration validity check this service has: every destination member needs a source
    /// member, an explicit <c>.Map(...)</c> or an explicit <c>.Ignore(...)</c>. This is what keeps the
    /// registers' <c>.Ignore(...)</c> calls load-bearing rather than decorative.
    /// </summary>
    /// <remarks>
    /// Observed failure mode: removing one <c>.Ignore(dest =&gt; dest.ZaakObjectType)</c> from the
    /// <c>BuurtZaakObject -&gt; BuurtZaakObjectRequestDto</c> config fails this fact alone, naming the pair
    /// and the member — "The following members of destination class
    /// OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject.BuurtZaakObjectRequestDto do not have a
    /// corresponding source member mapped or ignored:ZaakObjectType". That per-member detail is why the loop
    /// above compiles pair by pair and collects, rather than letting one <c>config.Compile()</c> throw on the
    /// first offender and hide the rest.
    /// </remarks>
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
