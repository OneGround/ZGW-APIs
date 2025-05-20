using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1.Queries;

public class ZaakTypeInformatieObjectTypenQueryParametersValidator : ZGWValidator<GetAllZaakTypeInformatieObjectTypenQueryParameters>
{
    public ZaakTypeInformatieObjectTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.InformatieObjectType).IsUri();
        CascadeRuleFor(p => p.Richting).IsEnumName(typeof(Richting));
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
