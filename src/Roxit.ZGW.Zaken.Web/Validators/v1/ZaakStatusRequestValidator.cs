using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

public class ZaakStatusRequestValidator : ZGWValidator<ZaakStatusRequestDto>
{
    public ZaakStatusRequestValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.StatusType).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.DatumStatusGezet).NotNull().NotEmpty().IsDateTime();
        CascadeRuleFor(z => z.StatusToelichting).MaximumLength(1000);
    }
}
