using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class OverigeZaakObjectRequestValidator : ZGWValidator<OverigeZaakObjectRequestDto>
{
    public OverigeZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new OverigeZaakObjectDtoValidator());
    }
}

public class OverigeZaakObjectDtoValidator : ZGWValidator<OverigeZaakObjectDto>
{
    public OverigeZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.OverigeData).NotNull().NotEmpty();
    }
}
