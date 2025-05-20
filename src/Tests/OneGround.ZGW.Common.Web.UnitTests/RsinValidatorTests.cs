using System.Linq;
using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests;

class RsinObject
{
    public string Rsin { get; set; }

    public RsinObject(string rsin)
    {
        Rsin = rsin;
    }
}

class RsinValidator : AbstractValidator<RsinObject>
{
    public RsinValidator()
    {
        RuleFor(r => r.Rsin).IsRsin(true);
    }
}

class RsinNullValidator : AbstractValidator<RsinObject>
{
    public RsinNullValidator()
    {
        RuleFor(r => r.Rsin).IsRsin(false);
    }
}

public class RsinNullValidatorTests
{
    private readonly RsinNullValidator _validator = new RsinNullValidator();

    [Fact]
    public void RsinChecker_With_Null_Allowed_Rsin_Should_Not_Give_Error()
    {
        var actual = _validator.Validate(new RsinObject(null));

        Assert.True(actual.IsValid, "Validation success expected");
        Assert.Empty(actual.Errors);
    }
}

public class RsinValidatorTests
{
    private readonly RsinValidator _validator = new RsinValidator();

    [Fact]
    public void RsinChecker_With_Null_Rsin_Should_Give_Error()
    {
        var actual = _validator.Validate(new RsinObject(null));

        Assert.False(actual.IsValid, "Validation error expected");
        Assert.Single(actual.Errors);
    }

    [Fact]
    public void RsinChecker_With_Empty_Rsin_Should_Give_Error()
    {
        var actual = _validator.Validate(new RsinObject(""));

        Assert.False(actual.IsValid, "Validation error expected");
        Assert.Single(actual.Errors);
    }

    [Fact]
    public void RsinChecker_With_Too_Short_Rsin_Should_Give_Error()
    {
        var actual = _validator.Validate(new RsinObject("1234"));

        Assert.False(actual.IsValid, "Validation error expected");
        Assert.Single(actual.Errors);
        Assert.Contains("9 tekens", actual.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void RsinChecker_With_Alphanumeric_Chars_In_Rsin_Should_Give_Error()
    {
        var actual = _validator.Validate(new RsinObject("123aa6789"));

        Assert.False(actual.IsValid, "Validation error expected");
        Assert.Single(actual.Errors);
        Assert.Contains("moet numeriek", actual.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void RsinChecker_With_Invalid_Rsin_Should_Give_Error()
    {
        var actual = _validator.Validate(new RsinObject("123456789")); // Not-ElfProef!

        Assert.False(actual.IsValid, "Validation error expected");
        Assert.Single(actual.Errors);
        Assert.Contains("Onjuist", actual.Errors.Single().ErrorMessage);
    }

    [Fact]
    public void RsinChecker_With_Valid_Rsin_Should_Return_Valid_With_No_Errors()
    {
        var actual = _validator.Validate(new RsinObject("605348157")); // ElfProef!

        Assert.True(actual.IsValid, "Validation success expected");
        Assert.Empty(actual.Errors);
    }
}
