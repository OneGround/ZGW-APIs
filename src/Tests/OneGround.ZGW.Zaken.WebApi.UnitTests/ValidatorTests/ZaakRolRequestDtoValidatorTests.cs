using FluentValidation;
using FluentValidation.TestHelper;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests;

public class ZaakRolRequestDtoValidatorTests
{
    private readonly ZaakRolRequestDtoValidator _validator;

    public ZaakRolRequestDtoValidatorTests()
    {
        // Mirrors the api wiring so error names resolve to their [JsonProperty] names.
        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Default;

        _validator = new ZaakRolRequestDtoValidator();
    }

    [Fact]
    public void ShouldHaveRequiredErrorWhenContactpersoonRolNaamIsNull()
    {
        var model = new ZaakRolRequestDto { ContactpersoonRol = new ContactpersoonRolDto { Naam = null } };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("contactpersoonRol.naam").WithErrorCode(ErrorCode.Required);
    }

    [Fact]
    public void ShouldHaveRequiredErrorWhenContactpersoonRolNaamIsOmitted()
    {
        var model = JsonConvert.DeserializeObject<ZaakRolRequestDto>("""{ "contactpersoonRol": { "functie": "Baliemedewerker" } }""");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("contactpersoonRol.naam").WithErrorCode(ErrorCode.Required);
    }

    [Fact]
    public void ShouldNotHaveErrorWhenContactpersoonRolNaamIsEmpty()
    {
        var model = new ZaakRolRequestDto { ContactpersoonRol = new ContactpersoonRolDto { Naam = "" } };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor("contactpersoonRol.naam");
    }

    [Fact]
    public void ShouldNotHaveErrorWhenContactpersoonRolNaamIsFilled()
    {
        var model = new ZaakRolRequestDto { ContactpersoonRol = new ContactpersoonRolDto { Naam = "Testpersoon" } };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor("contactpersoonRol.naam");
    }

    [Fact]
    public void ShouldNotHaveErrorWhenContactpersoonRolIsOmitted()
    {
        var model = new ZaakRolRequestDto { ContactpersoonRol = null };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor("contactpersoonRol.naam");
    }
}
