using FluentValidation.TestHelper;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Catalogi.Web.Validators.v1;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ValidatorTests;

public class ZaakTypeValidatorTests
{
    private readonly ZaakTypeRequestValidator _zaakTypeRequestValidator;

    public ZaakTypeValidatorTests()
    {
        _zaakTypeRequestValidator = new ZaakTypeRequestValidator();
    }

    [Fact]
    public void ShouldHaveErrorWhenIdentificatieHasNoncharsOrDigits()
    {
        var model = new ZaakTypeRequestDto() { Identificatie = "abc#$%^&" };

        var result = _zaakTypeRequestValidator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(person => person.Identificatie).WithErrorMessage("Waarde moet letters, cijfers of '_' en ' - ' bevatten");
    }

    [Fact]
    public void ShouldNotHaveErrorWhenIdentificatieIsCharsOrDigits()
    {
        var model = new ZaakTypeRequestDto() { Identificatie = "mytestidentificatie123" };

        var result = _zaakTypeRequestValidator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(person => person.Identificatie);
    }
}
