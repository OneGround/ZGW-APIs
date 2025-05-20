using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1.Queries;

public class RolTypenQueryParametersValidator : ZGWValidator<GetAllRolTypenQueryParameters>
{
    public RolTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.OmschrijvingGeneriek).IsEnumName(typeof(Common.DataModel.OmschrijvingGeneriek));
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
