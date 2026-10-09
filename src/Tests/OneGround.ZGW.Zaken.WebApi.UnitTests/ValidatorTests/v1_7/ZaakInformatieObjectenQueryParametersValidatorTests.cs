using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests.v1_7;

public class ZaakInformatieObjectenQueryParametersValidatorTests
{
    private readonly ZaakInformatieObjectenQueryParametersValidator _validator = new();

    [Fact]
    public void ShouldHaveErrorWhenZaakIsNotAUri()
    {
        var model = new GetAllZaakInformatieObjectenQueryParameters { Zaak = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.Zaak);
    }

    [Fact]
    public void ShouldHaveErrorWhenInformatieObjectIsNotAUri()
    {
        var model = new GetAllZaakInformatieObjectenQueryParameters { InformatieObject = "niet-een-url" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(p => p.InformatieObject);
    }

    [Fact]
    public void ShouldNotHaveErrorForValidModel()
    {
        var model = new GetAllZaakInformatieObjectenQueryParameters
        {
            Zaak = "https://zrc.test/zaken/11111111-1111-1111-1111-111111111111",
            InformatieObject = "https://drc.test/enkelvoudiginformatieobjecten/22222222-2222-2222-2222-222222222222",
            Expand = "informatieobject",
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveErrorWhenNothingIsGiven()
    {
        // Note: "either zaak or informatieobject should be specified" is a rule of the handler, not of this validator
        var result = _validator.TestValidate(new GetAllZaakInformatieObjectenQueryParameters());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void IsRegisteredLikeEveryOtherValidatorOfTheAssembly()
    {
        // Note: guards the actual reason for this validator: the one that is picked up for the v1.7 query type
        var services = new ServiceCollection();
        services.AddValidatorsFromAssembly(typeof(ZaakInformatieObjectenQueryParametersValidator).Assembly);

        Assert.Contains(
            services,
            d =>
                d.ServiceType == typeof(IValidator<GetAllZaakInformatieObjectenQueryParameters>)
                && d.ImplementationType == typeof(ZaakInformatieObjectenQueryParametersValidator)
        );
    }
}
