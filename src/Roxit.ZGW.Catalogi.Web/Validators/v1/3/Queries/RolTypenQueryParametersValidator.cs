using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Queries;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1._3.Queries;

public class RolTypenQueryParametersValidator : ZGWValidator<GetAllRolTypenQueryParameters>
{
    public RolTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.OmschrijvingGeneriek).IsEnumName(typeof(Common.DataModel.OmschrijvingGeneriek));
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
        CascadeRuleFor(p => p.DatumGeldigheid).IsDate(required: false);
    }
}
