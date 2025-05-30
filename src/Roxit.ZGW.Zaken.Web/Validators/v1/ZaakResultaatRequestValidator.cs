using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

public class ZaakResultaatRequestValidator : ZGWValidator<ZaakResultaatRequestDto>
{
    public ZaakResultaatRequestValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.ResultaatType).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Toelichting).MaximumLength(1000);
    }
}
