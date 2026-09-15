using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3.Queries;

public class ZaakTypenQueryParametersValidator : ZGWValidator<GetAllZaakTypenQueryParameters>
{
    public ZaakTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Catalogus).IsUri();
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
