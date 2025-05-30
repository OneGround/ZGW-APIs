using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1;

public class StatusTypeRequestValidator : ZGWValidator<StatusTypeRequestDto>
{
    public StatusTypeRequestValidator()
    {
        CascadeRuleFor(s => s.Omschrijving).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(s => s.OmschrijvingGeneriek).MaximumLength(80);
        CascadeRuleFor(s => s.StatusTekst).MaximumLength(80);
        CascadeRuleFor(s => s.ZaakType).NotNull().IsUri();
    }
}
