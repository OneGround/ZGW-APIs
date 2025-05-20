using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

public class ZaakContactmomentRequestDtoValidator : ZGWValidator<ZaakContactmomentRequestDto>
{
    public ZaakContactmomentRequestDtoValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Contactmoment).NotNull().NotEmpty().IsUri().MaximumLength(1000);
    }
}
