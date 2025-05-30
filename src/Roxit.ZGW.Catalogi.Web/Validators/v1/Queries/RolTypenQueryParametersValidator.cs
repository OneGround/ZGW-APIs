using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1.Queries;

public class RolTypenQueryParametersValidator : ZGWValidator<GetAllRolTypenQueryParameters>
{
    public RolTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.OmschrijvingGeneriek).IsEnumName(typeof(Common.DataModel.OmschrijvingGeneriek));
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
