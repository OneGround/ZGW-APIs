using FluentValidation;
using FluentValidation.TestHelper;
using OneGround.ZGW.Catalogi.Web.Validators.v1._4.Queries;
using Xunit;
using Q = OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ValidatorTests;

public class ZaakTypenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllZaakTypenQueryParameters>
{
    protected override IValidator<Q.GetAllZaakTypenQueryParameters> CreateValidator() => new ZaakTypenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllZaakTypenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllZaakTypenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/catalogussen/11111111-1111-1111-1111-111111111111", false)]
    public void Catalogus_MustBeAUrl(string catalogus, bool rejected)
    {
        var result = new ZaakTypenQueryParametersValidator().TestValidate(new Q.GetAllZaakTypenQueryParameters { Catalogus = catalogus });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "Catalogus"));
    }
}

public class InformatieObjectTypenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllInformatieObjectTypenQueryParameters>
{
    protected override IValidator<Q.GetAllInformatieObjectTypenQueryParameters> CreateValidator() =>
        new InformatieObjectTypenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllInformatieObjectTypenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllInformatieObjectTypenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/catalogussen/11111111-1111-1111-1111-111111111111", false)]
    public void Catalogus_MustBeAUrl(string catalogus, bool rejected)
    {
        var result = new InformatieObjectTypenQueryParametersValidator().TestValidate(
            new Q.GetAllInformatieObjectTypenQueryParameters { Catalogus = catalogus }
        );

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "Catalogus"));
    }
}

public class BesluitTypenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllBesluitTypenQueryParameters>
{
    protected override IValidator<Q.GetAllBesluitTypenQueryParameters> CreateValidator() => new BesluitTypenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllBesluitTypenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllBesluitTypenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/catalogussen/11111111-1111-1111-1111-111111111111", false)]
    public void Catalogus_MustBeAUrl(string catalogus, bool rejected)
    {
        var result = new BesluitTypenQueryParametersValidator().TestValidate(new Q.GetAllBesluitTypenQueryParameters { Catalogus = catalogus });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "Catalogus"));
    }

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/informatieobjecttypen/11111111-1111-1111-1111-111111111111", false)]
    public void InformatieObjectType_MustBeAUrl(string informatieObjectType, bool rejected)
    {
        var result = new BesluitTypenQueryParametersValidator().TestValidate(
            new Q.GetAllBesluitTypenQueryParameters { InformatieObjectType = informatieObjectType }
        );

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "InformatieObjectType"));
    }
}

public class EigenschappenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllEigenschappenQueryParameters>
{
    protected override IValidator<Q.GetAllEigenschappenQueryParameters> CreateValidator() => new EigenschappenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllEigenschappenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllEigenschappenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111", false)]
    public void ZaakType_MustBeAUrl(string zaakType, bool rejected)
    {
        var result = new EigenschappenQueryParametersValidator().TestValidate(new Q.GetAllEigenschappenQueryParameters { ZaakType = zaakType });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "ZaakType"));
    }
}

public class ResultaatTypenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllResultaatTypenQueryParameters>
{
    protected override IValidator<Q.GetAllResultaatTypenQueryParameters> CreateValidator() => new ResultaatTypenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllResultaatTypenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllResultaatTypenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111", false)]
    public void ZaakType_MustBeAUrl(string zaakType, bool rejected)
    {
        var result = new ResultaatTypenQueryParametersValidator().TestValidate(new Q.GetAllResultaatTypenQueryParameters { ZaakType = zaakType });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "ZaakType"));
    }
}

public class RolTypenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllRolTypenQueryParameters>
{
    protected override IValidator<Q.GetAllRolTypenQueryParameters> CreateValidator() => new RolTypenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllRolTypenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllRolTypenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111", false)]
    public void ZaakType_MustBeAUrl(string zaakType, bool rejected)
    {
        var result = new RolTypenQueryParametersValidator().TestValidate(new Q.GetAllRolTypenQueryParameters { ZaakType = zaakType });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "ZaakType"));
    }

    [Theory]
    [InlineData("nope", true)]
    [InlineData("initiator", false)]
    [InlineData("belanghebbende", false)]
    public void OmschrijvingGeneriek_MustBeAGenericDescription(string omschrijvingGeneriek, bool rejected)
    {
        var result = new RolTypenQueryParametersValidator().TestValidate(
            new Q.GetAllRolTypenQueryParameters { OmschrijvingGeneriek = omschrijvingGeneriek }
        );

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "OmschrijvingGeneriek"));
    }
}

public class StatusTypenQueryParametersValidatorTests : QueryParametersValidatorTestsBase<Q.GetAllStatusTypenQueryParameters>
{
    protected override IValidator<Q.GetAllStatusTypenQueryParameters> CreateValidator() => new StatusTypenQueryParametersValidator();

    protected override void SetStatus(Q.GetAllStatusTypenQueryParameters model, string status) => model.Status = status;

    protected override void SetDatumGeldigheid(Q.GetAllStatusTypenQueryParameters model, string datum) => model.DatumGeldigheid = datum;

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111", false)]
    public void ZaakType_MustBeAUrl(string zaakType, bool rejected)
    {
        var result = new StatusTypenQueryParametersValidator().TestValidate(new Q.GetAllStatusTypenQueryParameters { ZaakType = zaakType });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "ZaakType"));
    }
}

public class ZaakObjectTypenQueryParametersValidatorTests
{
    private readonly ZaakObjectTypenQueryParametersValidator _validator = new();

    [Theory]
    [InlineData("nope", true)]
    [InlineData("true", false)]
    [InlineData("false", false)]
    public void AnderObjectType_MustBeABoolean(string anderObjectType, bool rejected)
    {
        var result = _validator.TestValidate(new Q.GetAllZaakObjectTypenQueryParameters { AnderObjectType = anderObjectType });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "AnderObjectType"));
    }

    [Theory]
    [InlineData("DatumBeginGeldigheid")]
    [InlineData("DatumEindeGeldigheid")]
    [InlineData("DatumGeldigheid")]
    public void TheDates_ThatAreNotADate_AreRejected(string property)
    {
        var model = new Q.GetAllZaakObjectTypenQueryParameters();
        typeof(Q.GetAllZaakObjectTypenQueryParameters).GetProperty(property)!.SetValue(model, "not-a-date");

        _validator.TestValidate(model).ShouldHaveValidationErrorFor(property);
    }

    [Theory]
    [InlineData("DatumBeginGeldigheid")]
    [InlineData("DatumEindeGeldigheid")]
    [InlineData("DatumGeldigheid")]
    public void TheDates_ThatAreADateOrAbsent_AreAccepted(string property)
    {
        var model = new Q.GetAllZaakObjectTypenQueryParameters();
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(property);

        typeof(Q.GetAllZaakObjectTypenQueryParameters).GetProperty(property)!.SetValue(model, "2024-03-15");
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(property);
    }

    [Theory]
    [InlineData("Catalogus")]
    [InlineData("ZaakType")]
    [InlineData("ObjectType")]
    public void TheUrlFields_MustBeAUrl(string property)
    {
        var model = new Q.GetAllZaakObjectTypenQueryParameters();
        typeof(Q.GetAllZaakObjectTypenQueryParameters).GetProperty(property)!.SetValue(model, "not-a-url");
        _validator.TestValidate(model).ShouldHaveValidationErrorFor(property);

        typeof(Q.GetAllZaakObjectTypenQueryParameters)
            .GetProperty(property)!
            .SetValue(model, "https://ztc.test/resource/11111111-1111-1111-1111-111111111111");
        _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(property);
    }
}

public class ZaakTypeInformatieObjectTypenQueryParametersValidatorTests
{
    private readonly ZaakTypeInformatieObjectTypenQueryParametersValidator _validator = new();

    [Theory]
    [InlineData("not-a-url", true)]
    [InlineData("https://ztc.test/zaaktypen/11111111-1111-1111-1111-111111111111", false)]
    public void ZaakType_MustBeAUrl(string zaakType, bool rejected)
    {
        var result = _validator.TestValidate(new Q.GetAllZaakTypeInformatieObjectTypenQueryParameters { ZaakType = zaakType });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "ZaakType"));
    }

    [Theory]
    [InlineData("https://ztc.test/informatieobjecttypen/11111111-1111-1111-1111-111111111111", true)]
    [InlineData("Aanvraag", false)]
    public void InformatieObjectType_IsAnOmschrijvingAndNoUrl(string informatieObjectType, bool rejected)
    {
        var result = _validator.TestValidate(
            new Q.GetAllZaakTypeInformatieObjectTypenQueryParameters { InformatieObjectType = informatieObjectType }
        );

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "InformatieObjectType"));
    }

    [Fact]
    public void InformatieObjectType_LongerThan100Characters_IsRejected()
    {
        var result = _validator.TestValidate(
            new Q.GetAllZaakTypeInformatieObjectTypenQueryParameters { InformatieObjectType = new string('a', 101) }
        );

        Assert.Contains(result.Errors, e => e.PropertyName == "InformatieObjectType");
    }

    [Theory]
    [InlineData("nope", true)]
    [InlineData("inkomend", false)]
    [InlineData("intern", false)]
    [InlineData("uitgaand", false)]
    public void Richting_MustBeADirection(string richting, bool rejected)
    {
        var result = _validator.TestValidate(new Q.GetAllZaakTypeInformatieObjectTypenQueryParameters { Richting = richting });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "Richting"));
    }

    [Theory]
    [InlineData("nope", true)]
    [InlineData("alles", false)]
    [InlineData("concept", false)]
    [InlineData("definitief", false)]
    public void Status_MustBeAConceptStatus(string status, bool rejected)
    {
        var result = _validator.TestValidate(new Q.GetAllZaakTypeInformatieObjectTypenQueryParameters { Status = status });

        Assert.Equal(rejected, result.Errors.Exists(e => e.PropertyName == "Status"));
    }
}

public class CatalogussenQueryParametersValidatorTests
{
    private readonly CatalogussenQueryParametersValidator _validator = new();

    [Fact]
    public void Domein_LongerThanFiveCharacters_IsRejected()
    {
        _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Domein = "ABCDEF" }).ShouldHaveValidationErrorFor("Domein");
    }

    [Fact]
    public void Domein_OfFiveCharacters_IsAccepted()
    {
        _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Domein = "ABCDE" }).ShouldNotHaveValidationErrorFor("Domein");
    }

    [Fact]
    public void Rsin_ThatIsNotAnRsin_IsRejected()
    {
        _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Rsin = "123" }).ShouldHaveValidationErrorFor("Rsin");
    }

    [Fact]
    public void Rsin_ThatIsAnRsin_IsAccepted()
    {
        _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Rsin = "999993653" }).ShouldNotHaveValidationErrorFor("Rsin");
    }

    [Fact]
    public void ListOfDomeinen_WithOneThatIsTooLong_IsRejected()
    {
        var result = _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Domein__in = "ABC,ABCDEF" });

        Assert.Contains(result.Errors, e => e.PropertyName.Contains("domein__in"));
    }

    [Fact]
    public void ListOfRsins_WithOneThatIsNotAnRsin_IsRejected()
    {
        var result = _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Rsin__in = "999993653,123" });

        Assert.Contains(result.Errors, e => e.PropertyName.Contains("rsin__in"));
    }

    [Fact]
    public void ListsThatAreValid_AreAccepted()
    {
        var result = _validator.TestValidate(new Q.GetAllCatalogussenQueryParameters { Domein__in = "ABC,DEF", Rsin__in = "999993653,999990482" });

        Assert.True(result.Errors.Count == 0, string.Join("; ", result.Errors.ConvertAll(e => e.PropertyName + ": " + e.ErrorMessage)));
    }
}
