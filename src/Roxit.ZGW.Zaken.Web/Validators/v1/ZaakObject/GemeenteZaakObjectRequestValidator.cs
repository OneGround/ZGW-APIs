using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace Roxit.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class GemeenteZaakObjectRequestValidator : ZGWValidator<GemeenteZaakObjectRequestDto>
{
    public GemeenteZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new GemeenteZaakObjectDtoValidator());
    }
}

public class GemeenteZaakObjectDtoValidator : ZGWValidator<GemeenteZaakObjectDto>
{
    public GemeenteZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.GemeenteNaam).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(o => o.GemeenteCode).NotNull().NotEmpty().MaximumLength(4);
    }
}
