using FluentValidation.TestHelper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

public class ZaakObjectenQueryParametersValidatorTests
{
    private readonly ZaakObjectenQueryParametersValidator _validator = new();

    [Fact]
    public void ShouldHaveErrorWhenZaakIsNotAUri()
    {
        var model = new GetAllZaakObjectenQueryParameters { Zaak = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.Zaak);
    }

    [Fact]
    public void ShouldHaveErrorWhenObjectTypeIsNotAValidEnumName()
    {
        var model = new GetAllZaakObjectenQueryParameters { ObjectType = "niet-een-geldige-waarde" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.ObjectType);
    }

    [Fact]
    public void ShouldNotHaveErrorForValidModel()
    {
        var model = new GetAllZaakObjectenQueryParameters
        {
            Zaak = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111",
            Object = "https://example.test/objecten/1",
            ObjectType = "adres",
            Expand = "zaak",
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
