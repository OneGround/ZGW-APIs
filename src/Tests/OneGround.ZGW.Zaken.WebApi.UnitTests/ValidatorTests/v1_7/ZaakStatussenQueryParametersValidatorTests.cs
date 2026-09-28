using FluentValidation.TestHelper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

public class ZaakStatussenQueryParametersValidatorTests
{
    private readonly ZaakStatussenQueryParametersValidator _validator = new();

    [Fact]
    public void ShouldHaveErrorWhenZaakIsNotAUri()
    {
        var model = new GetAllZaakStatussenQueryParameters { Zaak = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.Zaak);
    }

    [Fact]
    public void ShouldNotHaveErrorForValidModel()
    {
        var model = new GetAllZaakStatussenQueryParameters
        {
            Zaak = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111",
            StatusType = "https://catalogi.test/statustypen/22222222-2222-2222-2222-222222222222",
            IndicatieLaatstGezetteStatus = "true",
            Expand = "zaak",
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
