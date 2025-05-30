using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1;

public class ZaakTypeInformatieObjectTypenRequestValidator : ZGWValidator<ZaakTypeInformatieObjectTypeRequestDto>
{
    public ZaakTypeInformatieObjectTypenRequestValidator()
    {
        CascadeRuleFor(r => r.ZaakType).NotNull().IsUri();
        CascadeRuleFor(r => r.InformatieObjectType).NotNull().IsUri();
        // Note: decided to make this optional (and generated automatically) in v1.0
        //CascadeRuleFor(r => r.VolgNummer).InclusiveBetween(1, 999);
        CascadeRuleFor(r => r.Richting).NotNull().IsEnumName(typeof(Richting));
        CascadeRuleFor(r => r.StatusType).IsUri();
    }
}
