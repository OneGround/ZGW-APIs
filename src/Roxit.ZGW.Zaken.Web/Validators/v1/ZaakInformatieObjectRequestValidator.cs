using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Requests;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

public class ZaakInformatieObjectRequestValidator : ZGWValidator<ZaakInformatieObjectRequestDto>
{
    public ZaakInformatieObjectRequestValidator()
    {
        CascadeRuleFor(z => z.InformatieObject).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Titel).MaximumLength(200);
    }
}
