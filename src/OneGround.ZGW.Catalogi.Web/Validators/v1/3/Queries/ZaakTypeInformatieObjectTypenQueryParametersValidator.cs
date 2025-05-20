using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3.Queries;

public class ZaakTypeInformatieObjectTypenQueryParametersValidator : ZGWValidator<GetAllZaakTypeInformatieObjectTypenQueryParameters>
{
    public ZaakTypeInformatieObjectTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.InformatieObjectType).IsNotUri().MaximumLength(100);
        CascadeRuleFor(p => p.Richting).IsEnumName(typeof(Richting));
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
