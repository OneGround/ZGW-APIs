using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Autorisaties.Web.Validators;

public class ApplicatieRequestValidator : ZGWValidator<ApplicatieRequestDto>
{
    public ApplicatieRequestValidator()
    {
        CascadeRuleFor(r => r.Label).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleForEach(r => r.Autorisaties)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(v => v.Component).NotNull().NotEmpty().IsEnumName(typeof(Component));
                validator.CascadeRuleFor(v => v.Scopes).NotNull();
                validator.CascadeRuleFor(v => v.ZaakType).IsUri().MaximumLength(1000);
                validator.CascadeRuleFor(v => v.InformatieObjectType).IsUri().MaximumLength(1000);
                validator.CascadeRuleFor(v => v.BesluitType).IsUri().MaximumLength(1000);
                validator
                    .CascadeRuleFor(v => v.MaxVertrouwelijkheidaanduiding)
                    .IsEnumName(typeof(VertrouwelijkheidAanduiding))
                    .Unless(v => string.IsNullOrEmpty(v.MaxVertrouwelijkheidaanduiding));
            });
        CascadeRuleFor(r => r.Autorisaties)
            .Custom(
                (a, c) =>
                {
                    ValidateAccessLevel(a, c);
                    ValidateScopesDefined(a, c);
                    ValidateOnUniqueComponentsAndScopes(a, c);
                }
            );
    }

    private static void ValidateAccessLevel(IEnumerable<AutorisatieRequestDto> autorisaties, ValidationContext<ApplicatieRequestDto> validatorCtx)
    {
        if (autorisaties == null)
        {
            return;
        }

        foreach (var authorization in autorisaties)
        {
            if (string.IsNullOrEmpty(authorization.Component) || !Enum.TryParse<Component>(authorization.Component, out _))
            {
                continue;
            }
            var isZrcOrDrc =
                authorization.Component.Equals(Component.zrc.ToString(), StringComparison.OrdinalIgnoreCase)
                || authorization.Component.Equals(Component.drc.ToString(), StringComparison.OrdinalIgnoreCase);

            var accessLevelValid =
                !string.IsNullOrEmpty(authorization.MaxVertrouwelijkheidaanduiding)
                && Enum.TryParse<VertrouwelijkheidAanduiding>(authorization.MaxVertrouwelijkheidaanduiding, out _);

            if (isZrcOrDrc && !accessLevelValid)
            {
                validatorCtx.AddFailure(
                    new FluentValidation.Results.ValidationFailure(
                        "MaxVertrouwelijkheidaanduiding",
                        $"Component '{authorization.Component}' has no valid access level."
                    )
                    {
                        ErrorCode = ErrorCode.Invalid,
                    }
                );
            }
            else if (!isZrcOrDrc && accessLevelValid)
            {
                validatorCtx.AddFailure(
                    new FluentValidation.Results.ValidationFailure(
                        "MaxVertrouwelijkheidaanduiding",
                        $"Component '{authorization.Component}' does not require an access level."
                    )
                    {
                        ErrorCode = ErrorCode.Invalid,
                    }
                );
            }
        }
    }

    private static void ValidateScopesDefined(IEnumerable<AutorisatieRequestDto> autorisaties, ValidationContext<ApplicatieRequestDto> validatorCtx)
    {
        if (autorisaties == null)
        {
            return;
        }

        var autorisatiesWithoutScopes = autorisaties.Where(a => a.Scopes == null || !a.Scopes.Any());

        foreach (var autorisatiesWithWithScope in autorisatiesWithoutScopes)
        {
            validatorCtx.AddFailure(
                new FluentValidation.Results.ValidationFailure(
                    "Scopes",
                    $"Component '{autorisatiesWithWithScope.Component}': 'scopes' has no elements."
                )
                {
                    ErrorCode = ErrorCode.Invalid,
                }
            );
        }
    }

    private static void ValidateOnUniqueComponentsAndScopes(
        IEnumerable<AutorisatieRequestDto> autorisaties,
        ValidationContext<ApplicatieRequestDto> validatorCtx
    )
    {
        if (autorisaties == null)
        {
            return;
        }

        var groupedByComponent = autorisaties.Where(a => a.Scopes != null).GroupBy(a => a.Component);

        var moreThanOneComponent = groupedByComponent.Where(group => group.Count() > 1).Select(g => g.Key);

        if (moreThanOneComponent.Any())
        {
            validatorCtx.AddFailure(
                new FluentValidation.Results.ValidationFailure(
                    "Component",
                    $"Component(s) '{string.Join(", ", moreThanOneComponent)}' defined more than once."
                )
                {
                    ErrorCode = ErrorCode.Unique,
                }
            );
        }

        foreach (var componentGroup in groupedByComponent)
        {
            var moreThanOneScope = componentGroup.SelectMany(a => a.Scopes).GroupBy(s => s).Where(s => s.Count() > 1).Select(s => s.Key);

            if (moreThanOneScope.Any())
            {
                validatorCtx.AddFailure(
                    new FluentValidation.Results.ValidationFailure(
                        "Scopes",
                        $"Component '{componentGroup.Key}': Scope(s) '{string.Join(", ", moreThanOneScope)}' defined more than once."
                    )
                    {
                        ErrorCode = ErrorCode.Unique,
                    }
                );
            }
        }
    }
}
