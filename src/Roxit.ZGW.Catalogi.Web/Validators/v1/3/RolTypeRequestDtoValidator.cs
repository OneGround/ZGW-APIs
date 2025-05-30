using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Requests;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1._3;

public class RolTypeRequestDtoValidator : ZGWValidator<RolTypeRequestDto>
{
    public RolTypeRequestDtoValidator()
    {
        CascadeRuleFor(r => r.ZaakType).NotNull().IsUri();
        CascadeRuleFor(r => r.Omschrijving).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(r => r.OmschrijvingGeneriek).NotNull().NotEmpty().IsEnumName(typeof(Common.DataModel.OmschrijvingGeneriek));
        CascadeRuleFor(z => z.BeginGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.BeginObject).IsDate(false);
        CascadeRuleFor(z => z.EindeObject).IsDate(false);
    }
}
