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
        ["zaaktype", "zaaktype.catalogus", "status", "status.statustype"]
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
        // "resultaat" is not registered as an expand resolver/entity for ZaakResponseDto yet (only
        // zaaktype/zaaktype.catalogus/status are, in this increment), so it must be rejected here too.
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""[{"resultaat": ["url"]}]"""));

        Assert.Null(error);
        Assert.Equal(["resultaat"], _validator.Validate(selection));
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
}
