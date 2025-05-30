using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakObject;

public class PandZaakObjectRequestValidator : ZGWValidator<PandZaakObjectRequestDto>
{
    public PandZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new PandZaakObjectDtoValidator());
    }
}

public class PandZaakObjectDtoValidator : ZGWValidator<Zaken.Contracts.v1.PandZaakObjectDto>
{
    public PandZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.Identificatie).NotNull().NotEmpty().MaximumLength(100);
    }
}
