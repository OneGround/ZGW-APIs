using FluentValidation.TestHelper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

public class ZaakResultatenQueryParametersValidatorTests
{
    private readonly ZaakResultatenQueryParametersValidator _validator = new();

    [Fact]
    public void ShouldHaveErrorWhenZaakIsNotAUri()
    {
        var model = new GetAllZaakResultatenQueryParameters { Zaak = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.Zaak);
    }

    [Fact]
    public void ShouldHaveErrorWhenResultaatTypeIsNotAUri()
    {
        var model = new GetAllZaakResultatenQueryParameters { ResultaatType = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.ResultaatType);
    }

    [Fact]
    public void ShouldNotHaveErrorForValidModel()
    {
        var model = new GetAllZaakResultatenQueryParameters
        {
            Zaak = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111",
            ResultaatType = "https://catalogi.test/resultaattypen/44444444-4444-4444-4444-444444444444",
            Expand = "zaak",
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
