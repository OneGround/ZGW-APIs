using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1.Queries;

public class BesluitTypenQueryParametersValidator : ZGWValidator<GetAllBesluitTypenQueryParameters>
{
    public BesluitTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Catalogus).IsUri();
        CascadeRuleFor(p => p.ZaakType).IsUri();
        CascadeRuleFor(p => p.InformatieObjectType).IsUri();
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
