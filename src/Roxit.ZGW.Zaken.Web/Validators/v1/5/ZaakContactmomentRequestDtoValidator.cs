using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5;

public class ZaakContactmomentRequestDtoValidator : ZGWValidator<ZaakContactmomentRequestDto>
{
    public ZaakContactmomentRequestDtoValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Contactmoment).NotNull().NotEmpty().IsUri().MaximumLength(1000);
    }
}
