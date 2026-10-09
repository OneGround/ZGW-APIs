using FluentValidation.TestHelper;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Web.Validators.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests;

public class ZaakSearchRequestValidatorTests
{
    private readonly ZaakSearchRequestValidator _validator = new();

    [Fact]
    public void ShouldHaveInvalidErrorWhenBothExpandAndFieldsAreSpecified()
    {
        var model = new ZaakSearchRequestDto { Expand = "zaaktype", Fields = new JArray("identificatie") };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("fields").WithErrorCode(ErrorCode.Invalid);
    }

    [Fact]
    public void ShouldNotHaveErrorWhenOnlyFieldsIsSpecified()
    {
        var model = new ZaakSearchRequestDto { Fields = new JArray("identificatie") };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor("fields");
    }

    [Fact]
    public void ShouldNotHaveErrorWhenOnlyExpandIsSpecified()
    {
        var model = new ZaakSearchRequestDto { Expand = "zaaktype" };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor("fields");
    }
}
