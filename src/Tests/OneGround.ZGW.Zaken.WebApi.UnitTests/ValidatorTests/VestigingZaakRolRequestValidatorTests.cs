using FluentValidation;
using FluentValidation.TestHelper;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests;

public class VestigingZaakRolRequestValidatorTests
{
    private readonly VestigingZaakRolRequestValidator _validator;

    public VestigingZaakRolRequestValidatorTests()
    {
        // Mirrors the api wiring so error names resolve to their [JsonProperty] names.
        ValidatorOptions.Global.PropertyNameResolver = PropertyNameResolver.Default;

        _validator = new VestigingZaakRolRequestValidator();
    }

    [Fact]
    public void ShouldHaveRequiredErrorWhenContactpersoonRolNaamIsNull()
    {
        var model = new VestigingZaakRolRequestDto { ContactpersoonRol = new ContactpersoonRolDto { Naam = null } };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("contactpersoonRol.naam").WithErrorCode(ErrorCode.Required);
    }

    [Fact]
    public void ShouldNotHaveErrorWhenContactpersoonRolNaamIsFilled()
    {
        var model = new VestigingZaakRolRequestDto { ContactpersoonRol = new ContactpersoonRolDto { Naam = "Testpersoon" } };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor("contactpersoonRol.naam");
    }
}
