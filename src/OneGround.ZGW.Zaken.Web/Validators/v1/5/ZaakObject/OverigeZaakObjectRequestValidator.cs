using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakObject;

public class OverigeZaakObjectRequestValidator : ZGWValidator<OverigeZaakObjectRequestDto>
{
    public OverigeZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new OverigeZaakObjectDtoValidator());
    }
}

public class OverigeZaakObjectDtoValidator : ZGWValidator<Zaken.Contracts.v1.OverigeZaakObjectDto>
{
    public OverigeZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.OverigeData).NotNull().NotEmpty();
    }
}
