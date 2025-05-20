using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1;

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
