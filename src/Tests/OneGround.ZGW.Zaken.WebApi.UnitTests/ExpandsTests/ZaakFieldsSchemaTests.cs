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
        ["zaaktype", "zaaktype.catalogus", "status", "status.statustype", "resultaat", "resultaat.resultaattype", "rollen", "rollen.roltype"]
    );

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
        // "hoofdzaak" is not registered as an expand resolver/entity for ZaakResponseDto yet (only
        // zaaktype/zaaktype.catalogus/status/resultaat and their nested types are, in this
        // increment), so it must be rejected here too.
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""[{"hoofdzaak": ["url"]}]"""));

        Assert.Null(error);
        Assert.Equal(["hoofdzaak"], _validator.Validate(selection));
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
}
