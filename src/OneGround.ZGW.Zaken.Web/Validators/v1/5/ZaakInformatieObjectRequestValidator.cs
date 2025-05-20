using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

public class ZaakInformatieObjectRequestValidator : ZGWValidator<ZaakInformatieObjectRequestDto>
{
    public ZaakInformatieObjectRequestValidator()
    {
        CascadeRuleFor(z => z.InformatieObject).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Titel).MaximumLength(200);
        CascadeRuleFor(z => z.VernietigingsDatum).IsDateTime();
        CascadeRuleFor(z => z.Status).MaximumLength(1000).IsUri();
    }
}
