using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace Roxit.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class PandZaakObjectRequestValidator : ZGWValidator<PandZaakObjectRequestDto>
{
    public PandZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new PandZaakObjectDtoValidator());
    }
}

public class PandZaakObjectDtoValidator : ZGWValidator<PandZaakObjectDto>
{
    public PandZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.Identificatie).NotNull().NotEmpty().MaximumLength(100);
    }
}
