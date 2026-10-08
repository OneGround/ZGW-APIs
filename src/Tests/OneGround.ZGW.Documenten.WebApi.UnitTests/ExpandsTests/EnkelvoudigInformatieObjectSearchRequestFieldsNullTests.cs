using FluentValidation.TestHelper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;
using OneGround.ZGW.Documenten.Web.Validators.v1._7;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.ExpandsTests;

/// <summary>
/// An explicit JSON null for "fields" means the same as leaving it out (clients that serialize unset properties as null).
/// </summary>
public class EnkelvoudigInformatieObjectSearchRequestFieldsNullTests
{
    private readonly EnkelvoudigInformatieObjectSearchRequestValidator _validator = new(new ConfigurationBuilder().Build());

    private static EnkelvoudigInformatieObjectSearchRequestDto Deserialize(string json) =>
        JsonConvert.DeserializeObject<EnkelvoudigInformatieObjectSearchRequestDto>(json);

    [Fact]
    public void Fields_ExplicitJsonNull_IsNull()
    {
        Assert.Null(Deserialize("""{ "fields": null }""").Fields);
    }

    [Fact]
    public void Fields_AssignedAJsonNullValue_IsNull()
    {
        var request = new EnkelvoudigInformatieObjectSearchRequestDto { Fields = JValue.CreateNull() };

        Assert.Null(request.Fields);
    }

    [Fact]
    public void Validate_ExpandTogetherWithFieldsNull_IsValid()
    {
        var result = _validator.TestValidate(Deserialize("""{ "expand": "informatieobjecttype", "fields": null }"""));

        result.ShouldNotHaveValidationErrorFor("fields");
    }

    [Fact]
    public void Validate_ExpandTogetherWithFields_IsStillRejected()
    {
        var result = _validator.TestValidate(Deserialize("""{ "expand": "informatieobjecttype", "fields": ["uuid"] }"""));

        result.ShouldHaveValidationErrorFor("fields");
    }
}
