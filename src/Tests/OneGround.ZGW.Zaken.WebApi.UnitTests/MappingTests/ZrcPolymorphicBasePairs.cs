using System;
using System.Linq;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// The four polymorphic base pairs and the subtype navigations their base configs must stay silent about,
/// in the <c>source -&gt; destination</c> notation both consuming facts use.
/// </summary>
/// <remarks>
/// Shared rather than restated per file because two facts have to agree on the same list or they stop
/// composing: <see cref="ZrcPolymorphicBaseConfigTests.The_polymorphic_base_configs_declare_no_rule_for_a_subtype_navigation"/>
/// forbids a rule on these members, and
/// <see cref="ZrcMapsterCompileTests.Every_registered_type_pair_maps_or_ignores_every_destination_member"/>
/// excludes exactly these members — and no others — from the completeness gate. A list that drifted between
/// the two would leave a member both un-gated and un-forbidden, which is the gap neither fact would report.
/// </remarks>
internal static class ZrcPolymorphicBasePairs
{
    private const string ZaakObjectDestination = "OneGround.ZGW.Zaken.DataModel.ZaakObject.ZaakObject";
    private const string ZaakRolDestination = "OneGround.ZGW.Zaken.DataModel.ZaakRol.ZaakRol";

    internal static readonly string[] ZaakObjectSubtypeNavigations =
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

    internal static readonly string[] ZaakRolSubtypeNavigations =
    [
        "NatuurlijkPersoon",
        "NietNatuurlijkPersoon",
        "Vestiging",
        "Medewerker",
        "OrganisatorischeEenheid",
    ];

    /// <summary>
    /// Both contract versions declare their own base request DTO and both map onto the single data model
    /// type, so each destination appears twice.
    /// </summary>
    internal static readonly (string Pair, string[] Navigations)[] All =
    [
        ($"OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject.ZaakObjectRequestDto -> {ZaakObjectDestination}", ZaakObjectSubtypeNavigations),
        ($"OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject.ZaakObjectRequestDto -> {ZaakObjectDestination}", ZaakObjectSubtypeNavigations),
        ($"OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol.ZaakRolRequestDto -> {ZaakRolDestination}", ZaakRolSubtypeNavigations),
        ($"OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol.ZaakRolRequestDto -> {ZaakRolDestination}", ZaakRolSubtypeNavigations),
    ];

    internal static string[] Names => All.Select(p => p.Pair).ToArray();

    internal static bool TryGetNavigations(string pair, out string[] navigations)
    {
        foreach (var (candidate, candidateNavigations) in All)
        {
            if (string.Equals(candidate, pair, StringComparison.Ordinal))
            {
                navigations = candidateNavigations;
                return true;
            }
        }

        navigations = null;
        return false;
    }
}
