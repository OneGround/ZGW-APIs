using System.Linq;
using FluentValidation.TestHelper;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Web.Validators.v1._7;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

/// <summary>
/// A medewerker identificatie of up to 254 characters can be stored on a rol, so it must also be usable as a search value.
/// </summary>
public class ZaakSearchRequestMedewerkerIdentificatieTests
{
    private readonly ZaakSearchRequestValidator _validator = new();

    [Theory]
    [InlineData(24, true)]
    [InlineData(254, true)]
    [InlineData(255, false)]
    public void Validate_MedewerkerIdentificatie_IsLimitedTo254(int length, bool valid)
    {
        var result = _validator.TestValidate(
            new ZaakSearchRequestDto { Rol__betrokkeneIdentificatie__medewerker__identificatie = new string('x', length) }
        );

        // Note: The rule comes from the shared search fields, so the casing of the reported property name is not asserted
        Assert.Equal(valid, !result.Errors.Any(e => e.PropertyName.ToLower() == "rol__betrokkeneidentificatie__medewerker__identificatie"));
    }
}
