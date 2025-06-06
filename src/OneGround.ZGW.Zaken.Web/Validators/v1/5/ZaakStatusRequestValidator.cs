using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

public class ZaakStatusRequestValidator : ZGWValidator<ZaakStatusRequestDto>
{
    public ZaakStatusRequestValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().MaximumLength(1000).IsUri();
        CascadeRuleFor(z => z.StatusType).NotNull().NotEmpty().MaximumLength(1000).IsUri();
        CascadeRuleFor(z => z.DatumStatusGezet).NotNull().NotEmpty().IsDateTime().NotInTheFuture();
        CascadeRuleFor(z => z.StatusToelichting).MaximumLength(1000);
        CascadeRuleFor(z => z.GezetDoor).MaximumLength(1000).IsUri();
    }
}
