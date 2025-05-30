using FluentValidation;
using Roxit.ZGW.Autorisaties.Contracts.v1.Requests;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Autorisaties.Web.Validators;

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
    }
}
