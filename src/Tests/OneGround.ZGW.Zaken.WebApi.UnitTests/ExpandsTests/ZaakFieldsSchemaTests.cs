using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakFieldsSchemaTests
{
    private readonly FieldsValidator<ZaakResponseDto> _validator = new(
        ZaakFieldsSchema.Build(),
        [
            "zaaktype",
            "zaaktype.catalogus",
            "status",
            "status.statustype",
            "resultaat",
            "resultaat.resultaattype",
            "rollen",
            "rollen.roltype",
            "zaakobjecten",
            "zaakobjecten.zaakobjecttype",
            "zaakcontactmomenten",
            "eigenschappen",
            "eigenschappen.eigenschap",
            "zaakinformatieobjecten",
            "zaakinformatieobjecten.informatieobject",
            "zaakinformatieobjecten.informatieobject.informatieobjecttype",
            "hoofdzaak",
            "hoofdzaak.zaaktype",
            "hoofdzaak.zaaktype.catalogus",
            "hoofdzaak.status",
            "hoofdzaak.status.statustype",
            "hoofdzaak.resultaat",
            "hoofdzaak.resultaat.resultaattype",
            "hoofdzaak.rollen",
            "hoofdzaak.rollen.roltype",
            "hoofdzaak.zaakobjecten",
            "hoofdzaak.zaakobjecten.zaakobjecttype",
            "hoofdzaak.zaakinformatieobjecten",
            "hoofdzaak.zaakinformatieobjecten.informatieobject",
            "hoofdzaak.deelzaken",
            "hoofdzaak.deelzaken.zaaktype",
            "hoofdzaak.deelzaken.status",
            "hoofdzaak.deelzaken.resultaat",
            "deelzaken",
            "deelzaken.zaaktype",
            "deelzaken.zaaktype.catalogus",
            "deelzaken.status",
            "deelzaken.status.statustype",
            "deelzaken.resultaat",
            "deelzaken.resultaat.resultaattype",
            "deelzaken.rollen",
            "deelzaken.rollen.roltype",
            "deelzaken.zaakobjecten",
            "deelzaken.zaakobjecten.zaakobjecttype",
            "deelzaken.zaakinformatieobjecten",
            "deelzaken.zaakinformatieobjecten.informatieobject",
            "relevanteanderezaken",
            "relevanteanderezaken.zaaktype",
            "relevanteanderezaken.status",
            "relevanteanderezaken.status.statustype",
            "relevanteanderezaken.resultaat",
            "relevanteanderezaken.resultaat.resultaattype",
            // Note: taken from the real resolvers, so a renamed path no longer silently keeps these tests green
            new ZaakCommunicatiekanaalResolver(null).Path,
            new ZaakSelectielijstklasseResolver(null).Path,
        ]
    );

    [Fact]
    public void Validate_ExternalJsonExpandsWithoutFieldSelection_AreValid()
    {
        var fields = JArray.Parse(
            """
            ["uuid", { "zaaktype": ["omschrijving", { "catalogus": ["domein"] }], "communicatiekanaal": [], "selectielijstklasse": [] }]
            """
        );

        var (selection, expandPaths, error) = FieldsParser.ParseAndValidate(fields);

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
        Assert.Contains("communicatiekanaal", expandPaths);
        Assert.Contains("selectielijstklasse", expandPaths);
    }

    [Theory]
    [InlineData("""[{ "hoofdzaak": [{ "communicatiekanaal": [] }] }]""", "hoofdzaak.communicatiekanaal")]
    [InlineData("""[{ "deelzaken": [{ "selectielijstklasse": [] }] }]""", "deelzaken.selectielijstklasse")]
    [InlineData("""[{ "relevanteanderezaken": [{ "communicatiekanaal": [] }] }]""", "relevanteanderezaken.communicatiekanaal")]
    [InlineData("""["communicatiekanaal.naam"]""", "communicatiekanaal")] // dotted path = inline nested object, which a url is not
    public void Validate_ExternalJsonExpandOnANestedOrInlinePath_IsInvalid(string fieldsJson, string expectedInvalid)
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse(fieldsJson));

        Assert.Null(error);
        Assert.Equal([expectedInvalid], _validator.Validate(selection));
    }

    [Theory]
    [InlineData("communicatiekanaal")]
    [InlineData("selectielijstklasse")]
    public void Validate_ExternalJsonExpandWithFieldSelection_IsInvalid(string name)
    {
        var fields = JArray.Parse($$"""[{ "{{name}}": ["naam"] }]""");

        var (selection, _, _) = FieldsParser.ParseAndValidate(fields);

        Assert.Equal([$"{name} (veldselectie wordt niet ondersteund)"], _validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaaktypeCatalogusNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"zaaktype": ["url", {"catalogus": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_UnknownScalarField_IsInvalid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["nietBestaandVeld"]"""));

        Assert.Null(error);
        Assert.Equal(["nietBestaandVeld"], _validator.Validate(selection));
    }

    [Fact]
    public void Validate_NonExpandableSubEntity_IsInvalid()
    {
        // "verlenging" is a real ZaakResponseDto property (an inline nested object, like
        // "betrokkeneIdentificatie"), but it's never registered as a FieldsSchema entity -- it's only
        // ever selected via dotted scalar paths ("verlenging.reden"), not object-entity syntax. So it
        // must be rejected here as a known-but-non-expandable field.
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""[{"verlenging": ["reden"]}]"""));

        Assert.Null(error);
        Assert.Equal(["verlenging"], _validator.Validate(selection));
    }

    [Fact]
    public void Validate_StatusFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"status": ["url", "datumStatusGezet"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_StatusStatustypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"status": ["url", {"statustype": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ResultaatFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"resultaat": ["url", "toelichting"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ResultaatResultaattypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"resultaat": ["url", {"resultaattype": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_RollenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"rollen": ["url", "betrokkene"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_RollenRoltypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"rollen": ["url", {"roltype": ["url"]}]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    // Discriminates that "betrokkeneIdentificatie" (an inline, polymorphic nested object -- its
    // shape depends on the ROL's betrokkeneType) is registered for every possible variant, not just
    // reflectable off the single RolResponseDto base type registered for "rollen" (which doesn't
    // declare BetrokkeneIdentificatie at all -- only its five concrete subtypes do).
    [Theory]
    [InlineData("vestigingsNummer")] // Vestiging
    [InlineData("kvknummer")] // Vestiging -- only on v1._5's VestigingZaakRolDto, not the v1 (base) one
    [InlineData("identificatie")] // Medewerker/NatuurlijkPersoon/NietNatuurlijkPersoon/OrganisatorischeEenheid
    public void Validate_RollenBetrokkeneIdentificatieNestedSelection_IsValid(string fieldName)
    {
        // Geneste velden van een inline object (niet-expandbaar, zoals betrokkeneIdentificatie) worden
        // geselecteerd via een gepunt pad ("betrokkeneIdentificatie.veld"), niet via object-syntax
        // ({"betrokkeneIdentificatie": [...]}) -- dat laatste is voorbehouden aan expandbare sub-entiteiten
        // (zoals "roltype"), zie FieldsParserTests.ParseAndValidate_DottedPath_PopulatesNestedObject_NoExpandPath.
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse($$"""["identificatie", {"rollen": ["url", "betrokkeneIdentificatie.{{fieldName}}"]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakobjectenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"zaakobjecten": ["url", "object"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakobjectenZaakobjecttypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"zaakobjecten": ["url", {"zaakobjecttype": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    // Discriminates that "objectIdentificatie" (an inline, polymorphic nested object -- its shape
    // depends on the ZAAKOBJECT's objectType) is registered for every possible variant, not just
    // reflectable off the single ZaakObjectResponseDto base type registered for "zaakobjecten" (which
    // doesn't declare ObjectIdentificatie at all -- only its eight concrete subtypes do).
    [Theory]
    [InlineData("identificatie")] // Adres/Buurt/Gemeente/KadastraleOnroerendeZaak/Pand/TerreinGebouwdObject
    [InlineData("waardepeildatum")] // WozWaarde
    public void Validate_ZaakobjectenObjectIdentificatieNestedSelection_IsValid(string fieldName)
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse($$"""["identificatie", {"zaakobjecten": ["url", "objectIdentificatie.{{fieldName}}"]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakcontactmomentenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"zaakcontactmomenten": ["url", "contactmoment"]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_EigenschappenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"eigenschappen": ["url", "waarde"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_EigenschappenEigenschapNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"eigenschappen": ["url", {"eigenschap": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakinformatieobjectenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"zaakinformatieobjecten": ["url", "titel"]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakinformatieobjectenInformatieobjectNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"zaakinformatieobjecten": ["url", {"informatieobject": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakinformatieobjectenInformatieobjectInformatieobjecttypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse(
                """["identificatie", {"zaakinformatieobjecten": ["url", {"informatieobject": ["url", {"informatieobjecttype": ["url"]}]}]}]"""
            )
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_ZaakinformatieobjectenInformatieobjectInformatieobjecttypeCatalogusNestedSelection_IsInvalid()
    {
        // "...informatieobjecttype.catalogus" would be a 4th nesting level from ZAAK, which exceeds
        // the VNG ZGW spec's 3-level expand cap -- so it must be rejected here too, same as
        // Validate_NonExpandableSubEntity_IsInvalid above.
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse(
                """["identificatie", {"zaakinformatieobjecten": ["url", {"informatieobject": ["url", {"informatieobjecttype": ["url", {"catalogus": ["url"]}]}]}]}]"""
            )
        );

        Assert.Null(error);
        Assert.Equal(["zaakinformatieobjecten.informatieobject.informatieobjecttype.catalogus"], _validator.Validate(selection));
    }

    [Fact]
    public void Validate_HoofdzaakFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"hoofdzaak": ["url", "identificatie"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_HoofdzaakStatusStatustypeNestedSelection_IsValid()
    {
        // Self-referential: "hoofdzaak" is itself a ZAAK, so its nested selections follow the exact
        // same schema as the top-level ZAAK (see Validate_StatusStatustypeNestedSelection_IsValid).
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"hoofdzaak": ["url", {"status": ["url", {"statustype": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_HoofdzaakZaaktypeCatalogusNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"hoofdzaak": ["url", {"zaaktype": ["url", {"catalogus": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_HoofdzaakDeelzakenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"hoofdzaak": ["url", {"deelzaken": ["url", "identificatie"]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Theory]
    [InlineData("zaaktype")]
    [InlineData("status")]
    [InlineData("resultaat")]
    public void Validate_HoofdzaakDeelzakenNestedSelection_IsValid(string nestedField)
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse($$"""["identificatie", {"hoofdzaak": ["url", {"deelzaken": ["url", {"{{nestedField}}": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_HoofdzaakDeelzakenZaaktypeCatalogusNestedSelection_IsInvalid()
    {
        // A 4th nesting level from ZAAK ("hoofdzaak.deelzaken.zaaktype.catalogus") exceeds the VNG ZGW
        // spec's 3-level expand cap -- see ZaakSelfReferenceExpandPaths.BuildDeelzakenUnder's own remarks.
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"hoofdzaak": ["url", {"deelzaken": ["url", {"zaaktype": ["url", {"catalogus": ["url"]}]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Equal(["hoofdzaak.deelzaken.zaaktype.catalogus"], _validator.Validate(selection));
    }

    [Fact]
    public void Validate_DeelzakenFieldSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""["identificatie", {"deelzaken": ["url", "identificatie"]}]"""));

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_DeelzakenStatusStatustypeNestedSelection_IsValid()
    {
        // Self-referential, same reasoning as Validate_HoofdzaakStatusStatustypeNestedSelection_IsValid.
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"deelzaken": ["url", {"status": ["url", {"statustype": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_DeelzakenZaakobjectenZaakobjecttypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"deelzaken": ["url", {"zaakobjecten": ["url", {"zaakobjecttype": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_RelevanteanderezakenStatusStatustypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"relevanteanderezaken": ["url", {"status": ["url", {"statustype": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_RelevanteanderezakenResultaatResultaattypeNestedSelection_IsValid()
    {
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"relevanteanderezaken": ["url", {"resultaat": ["url", {"resultaattype": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Empty(_validator.Validate(selection));
    }

    [Fact]
    public void Validate_RelevanteanderezakenZaaktypeCatalogusNestedSelection_IsInvalid()
    {
        // Unlike hoofdzaak/deelzaken, "relevanteanderezaken" deliberately does NOT support
        // ".zaaktype.catalogus" (matches the old v1._5 SupportedExpands scope for this resource).
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"relevanteanderezaken": ["url", {"zaaktype": ["url", {"catalogus": ["url"]}]}]}]""")
        );

        Assert.Null(error);
        Assert.Equal(["relevanteanderezaken.zaaktype.catalogus"], _validator.Validate(selection));
    }

    [Fact]
    public void Validate_RelevanteanderezakenRollenNestedSelection_IsInvalid()
    {
        // Unlike hoofdzaak/deelzaken, "relevanteanderezaken" deliberately does NOT support "rollen"
        // (nor zaakobjecten/zaakinformatieobjecten) -- matches the old v1._5 SupportedExpands scope.
        var (selection, _, error) = FieldsParser.ParseAndValidate(
            JArray.Parse("""["identificatie", {"relevanteanderezaken": ["url", {"rollen": ["url"]}]}]""")
        );

        Assert.Null(error);
        Assert.Equal(["relevanteanderezaken.rollen"], _validator.Validate(selection));
    }
}
