using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands;
using Xunit;
using V15 = OneGround.ZGW.Zaken.Contracts.v1._5;
using V15ZaakObject = OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;
using V15ZaakRol = OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;
using V1Rol = OneGround.ZGW.Zaken.Contracts.v1;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ContractTests;

/// <summary>
/// ZRC contract types as they come out of the expand mechanism. Properties that carry no explicit
/// [JsonProperty] name used to reach the client PascalCased, because expand projects the DTO to a
/// JObject itself instead of letting MVC's serializer name the properties.
///
/// Only ZRC 1.5 is covered: expand does not exist on 1.0 or 1.2.
/// </summary>
public class ExpandCasingTests
{
    // RvIG omnummertabel, Test-BSN 1240 - published as a test value and never issued to a person.
    private const string TestBsn = "999993653";

    // KvK test environment (developers.kvk.nl/documentation/testing) - fictional company.
    // A vestigingsnummer is 12 digits and its leading zeros are load-bearing; keep it a string.
    private const string TestVestigingsNummer = "000037178598";
    private const string TestKvkNummer = "69599084";

    // innNnpId is an RSIN. No Dutch authority publishes a reserved test range for one, and this
    // file lives in a public repository, so it is deliberately left unset rather than invented -
    // an RSIN that passes the elfproef is indistinguishable from a real organisation's. The key
    // still appears in the payload as null, which is all these assertions need.

    private static JObject Expand(object dto) => (JObject)DtoExpander.Merge(dto, new { _expand = new { roltype = new object() } });

    public static IEnumerable<object[]> Rollen()
    {
        // label, rol response DTO, a representative key inside the betrokkene identification object
        yield return ["natuurlijk_persoon", NatuurlijkPersoon(), "inpBsn"];
        yield return ["niet_natuurlijk_persoon", NietNatuurlijkPersoon(), "innNnpId"];
        yield return ["vestiging", Vestiging(), "vestigingsNummer"];
        yield return ["organisatorische_eenheid", OrganisatorischeEenheid(), "identificatie"];
        yield return ["medewerker", Medewerker(), "identificatie"];
    }

    // Same rollen, for the assertions that do not look inside the betrokkene identification object.
    public static IEnumerable<object[]> RollenWithoutInnerKey() => Rollen().Select(row => new[] { row[0], row[1] });

    [Theory]
    [MemberData(nameof(Rollen))]
    public void ExpandedRol_NamesTheBetrokkeneIdentificationInCamelCase(string label, object rol, string innerKey)
    {
        var expanded = Expand(rol);

        Assert.False(expanded.ContainsKey("BetrokkeneIdentificatie"), $"{label}: expand output still carries the C# member name.");
        Assert.True(expanded.TryGetValue("betrokkeneIdentificatie", out var token), $"{label}: expand output is missing 'betrokkeneIdentificatie'.");

        // Nothing inside the object moves: every member of these five betrokkene DTOs already
        // carries an explicit [JsonProperty] name, and an explicit name is left alone. That is a
        // fact about these five DTOs, not about the fix - elsewhere it does rename inner members,
        // ZaakObject's url and uuid among them.
        var betrokkeneIdentificatie = Assert.IsType<JObject>(token);

        Assert.True(betrokkeneIdentificatie.ContainsKey(innerKey), $"{label}: 'betrokkeneIdentificatie' is missing '{innerKey}'.");
        Assert.All(
            betrokkeneIdentificatie.Properties(),
            // Not char.IsLower: a leading underscore is legitimate (_expand, _error) and is not an
            // uppercase C# member name leaking through.
            property =>
                Assert.False(char.IsUpper(property.Name[0]), $"{label}: '{property.Name}' inside 'betrokkeneIdentificatie' is not camelCase.")
        );
    }

    [Theory]
    [MemberData(nameof(RollenWithoutInnerKey))]
    public void ExpandedRol_KeepsTheKeysThatWereAlreadyCorrect(string label, object rol)
    {
        var expanded = Expand(rol);

        Assert.True(expanded.ContainsKey("_expand"), $"{label}: the _expand key lost its leading underscore.");
        Assert.True(expanded.ContainsKey("betrokkeneType"), $"{label}: 'betrokkeneType' changed.");
        Assert.True(expanded.ContainsKey("roltype"), $"{label}: 'roltype' changed.");
    }

    /// <summary>
    /// ZGWJsonSerializer's converters have to survive the projection naming properties like MVC.
    /// </summary>
    [Fact]
    public void ExpandedRol_RendersGeboortedatumAsIsoDate()
    {
        var expanded = Expand(NatuurlijkPersoon());

        Assert.Equal("1970-01-01", (string)expanded["betrokkeneIdentificatie"]["geboortedatum"]);
    }

    [Fact]
    public void ExpandedZaakObject_NamesUrlAndUuidInCamelCase()
    {
        var zaakObject = new V15ZaakObject.AdresZaakObjectResponseDto
        {
            Url = "https://localhost/zaakobjecten/00000000-0000-0000-0000-000000000001",
            Uuid = Guid.Empty,
            ObjectType = "adres",
        };

        var expanded = Expand(zaakObject);

        Assert.False(expanded.ContainsKey("Url"));
        Assert.False(expanded.ContainsKey("Uuid"));
        Assert.True(expanded.ContainsKey("url"));
        Assert.True(expanded.ContainsKey("uuid"));
        Assert.True(expanded.ContainsKey("objectType"), "'objectType' was already correct and must not change.");
    }

    private static V15ZaakRol.NatuurlijkPersoonZaakRolResponseDto NatuurlijkPersoon() =>
        new()
        {
            BetrokkeneType = "natuurlijk_persoon",
            BetrokkeneIdentificatie = new V1Rol.NatuurlijkPersoonZaakRolDto
            {
                InpBsn = TestBsn,
                Geslachtsnaam = "Voorbeeld",
                Geboortedatum = "1970-01-01",
            },
        };

    private static V15ZaakRol.NietNatuurlijkPersoonZaakRolResponseDto NietNatuurlijkPersoon() =>
        new()
        {
            BetrokkeneType = "niet_natuurlijk_persoon",
            BetrokkeneIdentificatie = new V1Rol.NietNatuurlijkPersoonZaakRolDto { StatutaireNaam = "Voorbeeld B.V." },
        };

    private static V15ZaakRol.VestigingZaakRolResponseDto Vestiging() =>
        new()
        {
            BetrokkeneType = "vestiging",
            BetrokkeneIdentificatie = new V15.VestigingZaakRolDto
            {
                VestigingsNummer = TestVestigingsNummer,
                Handelsnaam = ["Voorbeeld"],
                KvKNummer = TestKvkNummer,
            },
        };

    private static V15ZaakRol.OrganisatorischeEenheidZaakRolResponseDto OrganisatorischeEenheid() =>
        new()
        {
            BetrokkeneType = "organisatorische_eenheid",
            BetrokkeneIdentificatie = new V1Rol.OrganisatorischeEenheidZaakRolDto { Identificatie = "Voorbeeld-OE-001", Naam = "Voorbeeld" },
        };

    private static V15ZaakRol.MedewerkerZaakRolResponseDto Medewerker() =>
        new()
        {
            BetrokkeneType = "medewerker",
            BetrokkeneIdentificatie = new V1Rol.MedewerkerZaakRolDto { Identificatie = "Voorbeeld-MW-001", Achternaam = "Voorbeeld" },
        };
}
