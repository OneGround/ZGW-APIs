using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakObject;

public class GemeenteZaakObjectRequestValidator : ZGWValidator<GemeenteZaakObjectRequestDto>
{
    public GemeenteZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new GemeenteZaakObjectDtoValidator());
    }
}

public class GemeenteZaakObjectDtoValidator : ZGWValidator<Zaken.Contracts.v1.GemeenteZaakObjectDto>
{
    public GemeenteZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.GemeenteNaam).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(o => o.GemeenteCode).NotNull().NotEmpty().MaximumLength(4);
    }
}
