using FluentValidation.TestHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Web.Validators.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

/// <summary>
/// An explicit JSON null for "fields" means the same as leaving it out (clients that serialize unset properties as null).
/// </summary>
public class ZaakSearchRequestFieldsNullTests
{
    private readonly ZaakSearchRequestValidator _validator = new();

    private static ZaakSearchRequestDto Deserialize(string json) => JsonConvert.DeserializeObject<ZaakSearchRequestDto>(json);

    [Fact]
    public void Fields_ExplicitJsonNull_IsNull()
    {
        Assert.Null(Deserialize("""{ "fields": null }""").Fields);
    }

    [Fact]
    public void Fields_AssignedAJsonNullValue_IsNull()
    {
        var request = new ZaakSearchRequestDto { Fields = JValue.CreateNull() };

        Assert.Null(request.Fields);
    }

    [Fact]
    public void Fields_AnArray_IsKept()
    {
        var request = Deserialize("""{ "fields": ["uuid"] }""");

        Assert.NotNull(request.Fields);
        Assert.Equal(JTokenType.Array, request.Fields.Type);
    }

    [Fact]
    public void Validate_ExpandTogetherWithFieldsNull_IsValid()
    {
        var result = _validator.TestValidate(Deserialize("""{ "expand": "zaaktype", "fields": null }"""));

        result.ShouldNotHaveValidationErrorFor("fields");
    }

    [Fact]
    public void Validate_ExpandTogetherWithFields_IsStillRejected()
    {
        var result = _validator.TestValidate(Deserialize("""{ "expand": "zaaktype", "fields": ["uuid"] }"""));

        result.ShouldHaveValidationErrorFor("fields");
    }
}
