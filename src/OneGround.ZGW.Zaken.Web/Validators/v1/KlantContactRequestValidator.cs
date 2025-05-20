using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;

namespace OneGround.ZGW.Zaken.Web.Validators.v1;

public class KlantContactRequestValidator : ZGWValidator<KlantContactRequestDto>
{
    public KlantContactRequestValidator()
    {
        CascadeRuleFor(k => k.Zaak).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(k => k.Identificatie).MaximumLength(14);
        CascadeRuleFor(k => k.DatumTijd).NotNull().NotEmpty().IsDateTime();
        CascadeRuleFor(k => k.Kanaal).MaximumLength(20);
        CascadeRuleFor(k => k.Onderwerp).MaximumLength(200);
        CascadeRuleFor(k => k.Toelichting).MaximumLength(1000);
    }
}
