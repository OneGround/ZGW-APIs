using System.Collections.Generic;
using FluentValidation.TestHelper;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Validators;
using OneGround.ZGW.Common.DataModel;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.ValidationTests;

public class ApplicatieRequestValidatorTests
{
    private readonly ApplicatieRequestValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Label_Is_Null_Or_Empty()
    {
        var model = new ApplicatieRequestDto { Label = null };
        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(a => a.Label);
    }

    [Fact]
    public void Should_Have_Error_When_Component_Is_Invalid_Enum()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto> { new AutorisatieRequestDto { Component = "InvalidEnumValue" } },
        };
        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(
            $"{nameof(ApplicatieRequestDto.Autorisaties)}.{model.Autorisaties.Count - 1}.{nameof(AutorisatieRequestDto.Component)}"
        );
    }

    [Fact]
    public void Should_Have_Error_When_Scopes_Is_Null()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto>
            {
                new AutorisatieRequestDto { Component = Component.zrc.ToString(), Scopes = null },
            },
        };
        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(
            $"{nameof(ApplicatieRequestDto.Autorisaties)}.{model.Autorisaties.Count - 1}.{nameof(AutorisatieRequestDto.Scopes)}"
        );
    }

    [Fact]
    public void Should_Have_Error_When_Scopes_Are_Duplicated()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto> { new AutorisatieRequestDto { Scopes = ["scope1", "scope1"] } },
        };
        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(nameof(AutorisatieRequestDto.Scopes));
    }

    [Fact]
    public void Should_Have_Error_When_Uri_Fields_Are_Invalid()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto>
            {
                new AutorisatieRequestDto { Component = Component.zrc.ToString(), ZaakType = "NotAUri" },
            },
        };
        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(
            $"{nameof(ApplicatieRequestDto.Autorisaties)}.{model.Autorisaties.Count - 1}.{nameof(AutorisatieRequestDto.ZaakType)}"
        );
    }

    [Fact]
    public void Should_Fail_When_ZrcOrDrc_Component_Without_AccessLevel()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto>
            {
                new AutorisatieRequestDto { Component = Component.zrc.ToString(), MaxVertrouwelijkheidaanduiding = null },
            },
        };

        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(nameof(AutorisatieRequestDto.MaxVertrouwelijkheidaanduiding));
    }

    [Fact]
    public void Should_Fail_When_NonZrcOrDrc_Component_With_AccessLevel()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto>
            {
                new AutorisatieRequestDto
                {
                    Component = Component.ztc.ToString(),
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                },
            },
        };

        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(nameof(AutorisatieRequestDto.MaxVertrouwelijkheidaanduiding));
    }

    [Fact]
    public void Should_Fail_When_Component_Are_Duplicated()
    {
        var model = new ApplicatieRequestDto
        {
            Autorisaties = new List<AutorisatieRequestDto>
            {
                new AutorisatieRequestDto
                {
                    Component = Component.zrc.ToString(),
                    Scopes = ["scope1"],
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                },
                new AutorisatieRequestDto
                {
                    Component = Component.zrc.ToString(),
                    Scopes = ["scope1"],
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                },
                new AutorisatieRequestDto
                {
                    Component = Component.drc.ToString(),
                    Scopes = ["scope1"],
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                },
            },
        };

        var result = _validator.TestValidate(model);

        Assert.False(result.IsValid);
        result.ShouldHaveValidationErrorFor(nameof(AutorisatieRequestDto.Component));
    }

    [Fact]
    public void Should_Pass_When_Is_Valid()
    {
        var dto = new ApplicatieRequestDto
        {
            Label = "Label",
            Autorisaties = new List<AutorisatieRequestDto>
            {
                new AutorisatieRequestDto
                {
                    Component = Component.zrc.ToString(),
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                    Scopes = ["scope1", "scope2"],
                },
                new AutorisatieRequestDto
                {
                    Component = Component.drc.ToString(),
                    MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.openbaar.ToString(),
                    Scopes = ["scope1", "scope2"],
                },
                new AutorisatieRequestDto { Component = Component.ac.ToString(), Scopes = ["scope1", "scope2"] },
                new AutorisatieRequestDto { Component = Component.brc.ToString(), Scopes = ["scope1", "scope2"] },
                new AutorisatieRequestDto { Component = Component.ztc.ToString(), Scopes = ["scope1", "scope2"] },
            },
        };

        var result = _validator.TestValidate(dto);

        Assert.True(result.IsValid);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
