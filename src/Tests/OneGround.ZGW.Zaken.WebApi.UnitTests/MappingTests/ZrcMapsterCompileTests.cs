using System;
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
    /// Runs the same completeness gate over the four polymorphic base pairs, excluding their subtype
    /// navigations — and ONLY those — so every other destination member on those pairs stays gated.
    /// </summary>
    /// <remarks>
    /// Those navigations cannot be annotated on the real config: the base request DTO has no source member
    /// for any of them, so the only annotation the gate would accept is an <c>.Ignore(...)</c> on the BASE
    /// config, and a base-config rule for a member wins over every derived config's rule for it — which
    /// silently blanks the identification object on every write through every subtype. Measured on these
    /// registers: mapping an address case object yields the identification with the base ignores absent,
    /// and null with them present. The inverse fact,
    /// <see cref="ZrcPolymorphicBaseConfigTests.The_polymorphic_base_configs_declare_no_rule_for_a_subtype_navigation"/>,
    /// forbids that rule on the real config; the two halves only work as a pair.
    /// <para>
    /// The exclusion is applied to a SEPARATE, throwaway config so it cannot reach production behaviour or
    /// the pairs compiled above — verified: the real config still maps the identification through. Excluding
    /// per member rather than skipping the whole pair is what closes the hole the earlier version had, where
    /// a newly added column on <c>ZaakObject</c>/<c>ZaakRol</c> (or a property on either base request DTO)
    /// would be neither gated here nor forbidden there, and would silently map to its default on the write
    /// path. <c>ForType</c> merges into the scanned rule rather than replacing it, so the base config's own
    /// existing <c>.Map</c>/<c>.Ignore</c> rules still count toward the gate.
    /// </para>
    /// </remarks>
    private static void GateBasePairsPerMember(List<(string Name, Type Source, Type Destination)> basePairs, List<string> unmapped)
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var probe = provider.GetRequiredService<TypeAdapterConfig>();
        probe.Default.RequireDestinationMemberSource(true);

        foreach (var (name, source, destination) in basePairs)
        {
            // Ignore(null) would throw rather than report, so fail with the pair name instead: the caller
            // only collects pairs this lookup already matched, so a miss here means the two have diverged.
            Assert.True(ZrcPolymorphicBasePairs.TryGetNavigations(name, out var navigations), $"No navigations recorded for {name}.");

            probe.ForType(source, destination).Ignore(navigations);
        }

        foreach (var (name, source, destination) in basePairs)
        {
            try
            {
                probe.Compile(source, destination);
            }
            catch (CompileException ex)
            {
                unmapped.Add($"{name}\n    {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }

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
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // Compile() over an empty RuleMap passes, so without this a broken config.Scan would turn the
        // gate green rather than red.
        Assert.NotEmpty(config.RuleMap);

        config.Compile();
    }

    /// <summary>
    /// The destination-member completeness gate: every destination member needs a source member, an explicit
    /// <c>.Map(...)</c> or an explicit <c>.Ignore(...)</c>. This is what keeps the registers'
    /// <c>.Ignore(...)</c> calls load-bearing rather than decorative.
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
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // On the test's own config, never inside AddZgwMapster: as a global seam setting it would also apply
        // to pairs with no register entry, which compile lazily on their first Map() call rather than at
        // startup -- so the failure would surface on a live request instead of here.
        config.Default.RequireDestinationMemberSource(true);

        var unmapped = new List<string>();
        var basePairsSeen = new List<(string Name, Type Source, Type Destination)>();

        // Per pair rather than one config.Compile(), which throws on the first failure and would make a
        // multi-member regression take several rounds to clear.
        foreach (var pair in config.RuleMap.Keys.OrderBy(k => k.Source.FullName).ThenBy(k => k.Destination.FullName).ToList())
        {
            var name = $"{pair.Source.FullName} -> {pair.Destination.FullName}";
            if (ZrcPolymorphicBasePairs.TryGetNavigations(name, out _))
            {
                basePairsSeen.Add((name, pair.Source, pair.Destination));
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

        // A base-pair name that matches no real pair is a dead string: the pair it was meant to cover
        // silently leaves the gate entirely. Assert the four were actually found — by identity rather than
        // by count, so a renamed pair cannot be masked by a second one matching twice. Sorted on both sides
        // because the loop's order is the RuleMap's, where "v1._5" sorts ahead of "v1.".
        Assert.Equal(
            ZrcPolymorphicBasePairs.Names.OrderBy(n => n, StringComparer.Ordinal),
            basePairsSeen.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
        );

        GateBasePairsPerMember(basePairsSeen, unmapped);

        Assert.True(
            unmapped.Count == 0,
            "These destination members have no source, no .Map(...) and no .Ignore(...). Map them, or "
                + "add an explicit .Ignore(...) recording that leaving them at their default is intended:\n  "
                + string.Join("\n  ", unmapped)
        );
    }
}
