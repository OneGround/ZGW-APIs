using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Expands.Fields;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Expands.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ExpandsTests;

public class ZaakFieldsSchemaTests
{
    private readonly FieldsValidator<ZaakResponseDto> _validator = new(ZaakFieldsSchema.Build(), ["zaaktype", "zaaktype.catalogus"]);

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
        // "status" is not registered as an expand resolver/entity for ZaakResponseDto yet (only
        // zaaktype/zaaktype.catalogus are, in this increment), so it must be rejected here too.
        var (selection, _, error) = FieldsParser.ParseAndValidate(JArray.Parse("""[{"status": ["url"]}]"""));

        Assert.Null(error);
        Assert.Equal(["status"], _validator.Validate(selection));
    }
}
