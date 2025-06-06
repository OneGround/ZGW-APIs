using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3;

public class ZaakObjectTypeRequestValidator : ZGWValidator<ZaakObjectTypeRequestDto>
{
    public ZaakObjectTypeRequestValidator()
    {
        CascadeRuleFor(s => s.BeginGeldigheid).IsDate(true);
        CascadeRuleFor(s => s.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(s => s.BeginObject).IsDate(false);
        CascadeRuleFor(s => s.EindeObject).IsDate(false);
        CascadeRuleFor(s => s.ObjectType).NotNull().MaximumLength(200).IsUri();
        CascadeRuleFor(s => s.RelatieOmschrijving).NotNull().MaximumLength(80);
        CascadeRuleFor(s => s.ZaakType).IsUri();
    }
}
