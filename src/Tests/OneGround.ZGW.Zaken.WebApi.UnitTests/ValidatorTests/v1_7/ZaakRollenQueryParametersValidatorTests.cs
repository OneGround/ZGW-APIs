using FluentValidation.TestHelper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

public class ZaakRollenQueryParametersValidatorTests
{
    private readonly ZaakRollenQueryParametersValidator _validator = new();

    [Fact]
    public void ShouldHaveErrorWhenZaakIsNotAUri()
    {
        var model = new GetAllZaakRollenQueryParameters { Zaak = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.Zaak);
    }

    [Fact]
    public void ShouldHaveErrorWhenBetrokkeneTypeIsNotAValidEnumName()
    {
        var model = new GetAllZaakRollenQueryParameters { BetrokkeneType = "niet-een-geldige-waarde" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.BetrokkeneType);
    }

    [Fact]
    public void ShouldNotHaveErrorForValidModel()
    {
        var model = new GetAllZaakRollenQueryParameters
        {
            Zaak = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111",
            Betrokkene = "https://example.test/betrokkenen/1",
            RolType = "https://catalogi.test/roltypen/22222222-2222-2222-2222-222222222222",
            BetrokkeneType = "medewerker",
            OmschrijvingGeneriek = "behandelaar",
            Expand = "zaak",
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
