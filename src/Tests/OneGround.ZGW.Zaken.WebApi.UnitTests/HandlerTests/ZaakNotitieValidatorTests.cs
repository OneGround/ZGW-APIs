using System.Linq;
using Microsoft.AspNetCore.Mvc;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.Web.Validators.v1._7;
using OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.HandlerTests;

public class ZaakNotitieValidatorTests
{
    private const string ZaakUrl = "https://zrc.test/api/v1/zaken/11111111-1111-1111-1111-111111111111";

    private static ZaakNotitieRequestDto Valid() =>
        new()
        {
            Onderwerp = "Onderwerp",
            Tekst = "Tekst",
            GerelateerdAan = ZaakUrl,
        };

    private static bool HasError(FluentValidation.Results.ValidationResult result, string property) =>
        result.Errors.Any(e => e.PropertyName.ToLower() == property);

    [Fact]
    public void Request_WithTheRequiredFieldsOnly_IsValid()
    {
        Assert.True(new ZaakNotitieRequestValidator().Validate(Valid()).IsValid);
    }

    [Theory]
    [InlineData(null, "tekst", ZaakUrl, "onderwerp")]
    [InlineData("", "tekst", ZaakUrl, "onderwerp")]
    [InlineData("onderwerp", null, ZaakUrl, "tekst")]
    [InlineData("onderwerp", "", ZaakUrl, "tekst")]
    [InlineData("onderwerp", "tekst", null, "gerelateerdaan")]
    [InlineData("onderwerp", "tekst", "geen-url", "gerelateerdaan")]
    public void Request_WithoutARequiredField_IsInvalid(string onderwerp, string tekst, string gerelateerdAan, string expectedProperty)
    {
        var result = new ZaakNotitieRequestValidator().Validate(
            new ZaakNotitieRequestDto
            {
                Onderwerp = onderwerp,
                Tekst = tekst,
                GerelateerdAan = gerelateerdAan,
            }
        );

        Assert.True(HasError(result, expectedProperty));
    }

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public void Request_Onderwerp_IsLimitedTo255(int length, bool valid)
    {
        var request = Valid();
        request.Onderwerp = new string('x', length);

        Assert.Equal(valid, !HasError(new ZaakNotitieRequestValidator().Validate(request), "onderwerp"));
    }

    [Theory]
    [InlineData(255, true)]
    [InlineData(256, false)]
    public void Request_AangemaaktDoor_IsLimitedTo255(int length, bool valid)
    {
        var request = Valid();
        request.AangemaaktDoor = new string('x', length);

        Assert.Equal(valid, !HasError(new ZaakNotitieRequestValidator().Validate(request), "aangemaaktdoor"));
    }

    [Fact]
    public void Request_Tekst_HasNoMaximumLength()
    {
        var request = Valid();
        request.Tekst = new string('x', 100_000);

        Assert.True(new ZaakNotitieRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData("intern", true)]
    [InlineData("extern", true)]
    [InlineData("onbekend", false)]
    public void Request_NotitieType_MustBeInternOrExtern(string value, bool valid)
    {
        var request = Valid();
        request.NotitieType = value;

        Assert.Equal(valid, !HasError(new ZaakNotitieRequestValidator().Validate(request), "notitietype"));
    }

    [Theory]
    [InlineData("concept", true)]
    [InlineData("definitief", true)]
    [InlineData("onbekend", false)]
    public void Request_Status_MustBeConceptOrDefinitief(string value, bool valid)
    {
        var request = Valid();
        request.Status = value;

        Assert.Equal(valid, !HasError(new ZaakNotitieRequestValidator().Validate(request), "status"));
    }

    [Fact]
    public void Query_WithoutParameters_IsValid()
    {
        Assert.True(new ZaakNotitiesQueryParametersValidator().Validate(new GetAllZaakNotitiesQueryParameters()).IsValid);
    }

    [Theory]
    [InlineData("gerelateerdAan", "geen-url")]
    [InlineData("notitieType", "onbekend")]
    [InlineData("status", "onbekend")]
    [InlineData("aanmaakdatum__gt", "geen-datum")]
    [InlineData("wijzigingsdatum__lte", "geen-datum")]
    [InlineData("pageSize", "0")]
    [InlineData("pageSize", "abc")]
    public void Query_WithAnInvalidValue_IsInvalid(string parameter, string value)
    {
        var query = new GetAllZaakNotitiesQueryParameters();
        var property = typeof(GetAllZaakNotitiesQueryParameters)
            .GetProperties()
            .Single(p => p.GetCustomAttributes(typeof(FromQueryAttribute), false).Cast<FromQueryAttribute>().Any(a => a.Name == parameter));
        property.SetValue(query, value);

        Assert.False(new ZaakNotitiesQueryParametersValidator().Validate(query).IsValid);
    }

    [Fact]
    public void Query_WithValidValues_IsValid()
    {
        var query = new GetAllZaakNotitiesQueryParameters
        {
            GerelateerdAan = ZaakUrl,
            NotitieType = "extern",
            Status = "concept",
            Aanmaakdatum__gte = "2026-10-01T08:00:00Z",
            PageSize = "500",
        };

        Assert.True(new ZaakNotitiesQueryParametersValidator().Validate(query).IsValid);
    }
}
