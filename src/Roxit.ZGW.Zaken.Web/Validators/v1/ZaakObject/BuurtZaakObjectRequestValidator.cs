using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace Roxit.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class BuurtZaakObjectRequestValidator : ZGWValidator<BuurtZaakObjectRequestDto>
{
    public BuurtZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new BuurtZaakObjectDtoValidator());
    }
}

public class BuurtZaakObjectDtoValidator : ZGWValidator<BuurtZaakObjectDto>
{
    public BuurtZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.BuurtCode).NotNull().NotEmpty().MaximumLength(2);
        CascadeRuleFor(o => o.BuurtNaam).NotNull().NotEmpty().MaximumLength(40);
        CascadeRuleFor(o => o.GemGemeenteCode).NotNull().NotEmpty().MaximumLength(4);
        CascadeRuleFor(o => o.WykWijkCode).NotNull().NotEmpty().MaximumLength(2);
    }
}
