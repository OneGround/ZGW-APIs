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
    private static readonly string[] ZaakObjectSubtypeNavigations =
    [
        "Adres",
        "Buurt",
        "Pand",
        "KadastraleOnroerendeZaak",
        "Gemeente",
        "TerreinGebouwdObject",
        "Overige",
        "WozWaardeObject",
    ];

    private static readonly string[] ZaakRolSubtypeNavigations =
    [
        "NatuurlijkPersoon",
        "NietNatuurlijkPersoon",
        "Vestiging",
        "Medewerker",
        "OrganisatorischeEenheid",
    ];

    /// <summary>
    /// The four polymorphic base pairs, in the same <c>source -&gt; destination</c> notation the
    /// completeness gate's exclusion list uses. Both contract versions declare their own base request DTO
    /// and both map onto the single data model type, so each destination appears twice.
    /// </summary>
    private static readonly (string Pair, string[] Navigations)[] BasePairs =
    [
        (
            "OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.ZaakObjectRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakObject.ZaakObject",
            ZaakObjectSubtypeNavigations
        ),
        (
            "OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakObject.ZaakObject",
            ZaakObjectSubtypeNavigations
        ),
        (
            "OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol.ZaakRolRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol",
            ZaakRolSubtypeNavigations
        ),
        (
            "OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol.ZaakRolRequestDto -> OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol",
            ZaakRolSubtypeNavigations
        ),
    ];

    /// <summary>
    /// A base config must declare no rule at all — neither <c>.Map</c> nor <c>.Ignore</c> — for a member
    /// that a derived config maps. Measured on these registers: an address case object maps its
    /// identification through, and comes out null once the base config ignores the subtype navigations.
    /// </summary>
    /// <remarks>
    /// This is deliberately the inverse of the destination-member completeness gate in
    /// <see cref="ZrcMapsterCompileTests.Every_registered_type_pair_maps_or_ignores_every_destination_member"/>,
    /// which would accept only the annotation this forbids. That is why those four pairs are excluded there
    /// and asserted here instead — the exclusion is safe exactly as long as this fact holds.
    /// </remarks>
    [Fact]
    public void The_polymorphic_base_configs_declare_no_rule_for_a_subtype_navigation()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

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

            foreach (var ignored in settings.Ignore.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                if (navigations.Contains(RootMember(ignored)))
                {
                    offences.Add($"{pair}\n      .Ignore(dest => dest.{ignored})");
                }
            }

            foreach (var resolver in settings.Resolvers)
            {
                if (resolver.DestinationMemberName is { } member && navigations.Contains(RootMember(member)))
                {
                    offences.Add($"{pair}\n      .Map(dest => dest.{member}, ...)");
                }
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
