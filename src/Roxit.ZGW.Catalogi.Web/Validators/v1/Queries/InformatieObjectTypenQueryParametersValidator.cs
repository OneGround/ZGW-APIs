using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1.Queries;

public class InformatieObjectTypenQueryParametersValidator : ZGWValidator<GetAllInformatieObjectTypenQueryParameters>
{
    public InformatieObjectTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Catalogus).IsUri();
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
    }
}
