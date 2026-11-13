using FluentValidation;
using OneGround.ZGW.Zaken.Web.Validators;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.ValidatorTests;

public class ZaakUrlRuleBuilderExtensionsTests
{
    private class Model
    {
        public string Url { get; set; }
    }

    private class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator()
        {
            RuleFor(m => m.Url).ContainsZaakId();
        }
    }

    [Theory]
    [InlineData("https://example.test/api/v1/zaken/3f1c2a5e-0007-4000-8000-000000000001")]
    [InlineData("https://example.test/api/v1/zaken/3f1c2a5e-0007-4000-8000-000000000001/")]
    [InlineData(null)]
    [InlineData("")]
    public void A_zaak_url_or_an_empty_value_is_valid(string url)
    {
        Assert.True(new ModelValidator().Validate(new Model { Url = url }).IsValid);
    }

    [Theory]
    [InlineData("https://example.test/api/v1/zaken/no-guid")]
    [InlineData("https://example.test/api/v1/enkelvoudiginformatieobjecten/3f1c2a5e-0007-4000-8000-000000000001")]
    [InlineData("https://example.test/api/v1/zaken/3f1c2a5e-0007-4000-8000-000000000001/rollen/3f1c2a5e-0007-4000-8000-000000000002")]
    [InlineData("not a url")]
    public void An_url_that_does_not_contain_a_zaak_id_is_invalid(string url)
    {
        var result = new ModelValidator().Validate(new Model { Url = url });

        Assert.False(result.IsValid);
    }
}
