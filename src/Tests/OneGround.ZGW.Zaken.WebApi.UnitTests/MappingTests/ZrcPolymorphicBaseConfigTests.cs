using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// Mapster resolves a map on <c>source.GetType()</c>, so for a polymorphic request DTO both the base
/// config and the concrete subtype's config apply. A rule on the BASE config for a member — <c>.Map</c> or
/// <c>.Ignore</c>, either one — wins over every derived config's rule for that same member, for every
/// source type in the hierarchy. One <c>.Ignore(dest =&gt; dest.Adres)</c> on the base config therefore
/// blanks the identification object on every write through every subtype, with no exception, no log entry
/// and no other failing test. These facts forbid that rule and assert the behaviour it would break.
/// </summary>
public class ZrcPolymorphicBaseConfigTests
{
    /// <summary>
    /// The four polymorphic base pairs and their subtype navigations, taken from the single list
    /// <see cref="ZrcPolymorphicBasePairs"/> that the completeness gate's per-member exclusion also reads —
    /// the two facts only compose while they agree on the same members.
    /// </summary>
    private static (string Pair, string[] Navigations)[] BasePairs => ZrcPolymorphicBasePairs.All;

    /// <summary>
    /// A base config must declare no rule at all — neither <c>.Map</c> nor <c>.Ignore</c> — for a member
    /// that a derived config maps. Measured on these registers: an address case object maps its
    /// identification through, and comes out null once the base config ignores the subtype navigations.
    /// </summary>
    /// <remarks>
    /// This is deliberately the inverse of the destination-member completeness gate in
    /// <see cref="ZrcMapsterCompileTests.Every_registered_type_pair_maps_or_ignores_every_destination_member"/>,
    /// which would accept only the annotation this forbids. That is why the gate excludes these subtype
    /// navigations — and only these members, not the whole pair — and asserts them here instead; the
    /// exclusion is safe exactly as long as this fact holds.
    /// <para>
    /// Observed failure mode: adding <c>.Ignore(dest =&gt; dest.Adres)</c> to the
    /// <c>ZaakObjectRequestDto -&gt; ZaakObject</c> base config fails with the offending pair AND the
    /// offending rule spelled out — "OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.ZaakObjectRequestDto
    /// -&gt; OneGround.ZGW.Zaken.DataModel.ZaakObject.ZaakObject / .Ignore(dest =&gt; dest.Adres)". Naming the
    /// rule, not just the pair, is the payoff for reading <c>RuleMap</c> structurally instead of inferring the
    /// violation from behaviour. Reproduced independently from a clean tree with byte-identical output.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_polymorphic_base_configs_declare_no_rule_for_a_subtype_navigation()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        // A broken config.Scan leaves the RuleMap empty, which would make every lookup below miss and turn
        // this fact green on nothing at all.
        Assert.NotEmpty(config.RuleMap);

        var registered = config.RuleMap.ToDictionary(e => $"{e.Key.Source.FullName} -> {e.Key.Destination.FullName}", e => e);
        var found = new List<string>();
        var offences = new List<string>();

        foreach (var (pair, navigations) in BasePairs)
        {
            if (!registered.TryGetValue(pair, out var entry))
            {
                continue;
            }

            found.Add(pair);

            // A renamed navigation would leave this fact scanning for a member that no longer exists, so the
            // names are checked against the destination type rather than trusted as string constants.
            var absent = navigations.Where(n => entry.Key.Destination.GetProperty(n) == null).ToList();
            Assert.True(
                absent.Count == 0,
                $"{entry.Key.Destination.FullName} no longer declares these navigations, so the names below check nothing: "
                    + string.Join(", ", absent)
            );

            var settings = entry.Value.Settings;

            foreach (var ignored in settings.Ignore.Keys.Where(k => navigations.Contains(RootMember(k))).OrderBy(k => k, StringComparer.Ordinal))
            {
                offences.Add($"{pair}\n      .Ignore(dest => dest.{ignored})");
            }

            foreach (
                var member in settings.Resolvers.Select(r => r.DestinationMemberName).Where(m => m is not null && navigations.Contains(RootMember(m)))
            )
            {
                offences.Add($"{pair}\n      .Map(dest => dest.{member}, ...)");
            }
        }

        // A pair name that matches nothing leaves that pair unchecked, and a vacuously green guard here is
        // exactly the failure this fact exists to prevent: the completeness gate excludes these same four
        // pairs on the strength of it.
        Assert.Equal(BasePairs.Select(p => p.Pair).ToArray(), found.ToArray());

        Assert.True(
            offences.Count == 0,
            "A polymorphic base config declares a rule for a member that its derived configs map. A rule on the base config wins over every "
                + "derived config's rule for that member, for every source type in the hierarchy, so this blanks the identification object on "
                + "writes through every subtype without raising anything. Move the rule onto the derived configs:\n  "
                + string.Join("\n  ", offences)
        );
    }

    /// <summary>
    /// The behaviour the fact above protects, asserted directly so a reader can see what is at stake without
    /// reconstructing the argument: a request typed as a concrete subtype still carries its identification
    /// object into the data model.
    /// </summary>
    /// <remarks>
    /// Observed failure mode: with the same single <c>.Ignore(dest =&gt; dest.Adres)</c> on the base config
    /// this fails on <c>Assert.NotNull() Failure: Value is null</c> — the derived config's
    /// <c>.Map(dest =&gt; dest.Adres, src =&gt; src.ObjectIdentificatie)</c> is still registered and simply
    /// loses, with no exception and no log line. It fails on the null rather than on the pinned value because
    /// a base ignore blanks the whole navigation; the pinned-value assertion on the line after is what would
    /// catch a narrower regression that produced the object but dropped its contents.
    /// </remarks>
    [Fact]
    public void A_derived_case_object_map_still_produces_its_identification()
    {
        using var host = new ZrcMapperTestHost();

        var result = host.Mapper.Map<ZaakObject>(
            new AdresZaakObjectRequestDto
            {
                Object = "https://example.test/objects/1",
                ObjectType = ObjectType.adres.ToString(),
                ObjectIdentificatie = new AdresZaakObjectDto { Identificatie = "PINNED-ID", Huisnummer = 42 },
            }
        );

        Assert.NotNull(result.Adres);
        Assert.Equal("PINNED-ID", result.Adres.Identificatie);
    }

    /// <summary>
    /// Mapster records a rule for a nested path under the root member's name followed by the path, so a rule
    /// reaching into a subtype navigation counts as a rule on that navigation.
    /// </summary>
    private static string RootMember(string memberPath)
    {
        var separator = memberPath.IndexOf('.');
        return separator < 0 ? memberPath : memberPath[..separator];
    }
}
