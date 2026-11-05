using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._4.Queries;

public class StatusTypenQueryParametersValidator : ZGWValidator<GetAllStatusTypenQueryParameters>
{
    public StatusTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
        CascadeRuleFor(p => p.DatumGeldigheid).IsDate(required: false);
    }
}
