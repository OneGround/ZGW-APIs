using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

public class ZaakEigenschapRequestValidator : ZGWValidator<ZaakEigenschapRequestDto>
{
    public ZaakEigenschapRequestValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Eigenschap).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Waarde).NotNull().NotEmpty();
    }
}
