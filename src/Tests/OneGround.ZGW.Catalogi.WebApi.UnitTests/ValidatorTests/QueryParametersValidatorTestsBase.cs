using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ValidatorTests;

/// <summary>
/// The rules that the v1.4 query validators of the lists of types have in common: the <c>status</c> is one of the concept statuses and the
/// <c>datumGeldigheid</c> is a date (or absent). What differs per validator is only the model.
/// </summary>
public abstract class QueryParametersValidatorTestsBase<TModel>
    where TModel : class, new()
{
    protected abstract IValidator<TModel> CreateValidator();

    protected abstract void SetStatus(TModel model, string status);

    protected abstract void SetDatumGeldigheid(TModel model, string datum);

    private TestValidationResult<TModel> Validate(TModel model) => CreateValidator().TestValidate(model);

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2024-13-45")]
    [InlineData("15-03-2024")]
    public void DatumGeldigheid_ThatIsNotADate_IsRejected(string datum)
    {
        var model = new TModel();
        SetDatumGeldigheid(model, datum);

        Validate(model).ShouldHaveValidationErrorFor("DatumGeldigheid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2024-03-15")]
    public void DatumGeldigheid_ThatIsADateOrAbsent_IsAccepted(string datum)
    {
        var model = new TModel();
        SetDatumGeldigheid(model, datum);

        Validate(model).ShouldNotHaveValidationErrorFor("DatumGeldigheid");
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("Alles2")]
    public void Status_ThatIsNotAConceptStatus_IsRejected(string status)
    {
        var model = new TModel();
        SetStatus(model, status);

        Validate(model).ShouldHaveValidationErrorFor("Status");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("alles")]
    [InlineData("concept")]
    [InlineData("definitief")]
    public void Status_ThatIsAConceptStatusOrAbsent_IsAccepted(string status)
    {
        var model = new TModel();
        SetStatus(model, status);

        Validate(model).ShouldNotHaveValidationErrorFor("Status");
    }
}
