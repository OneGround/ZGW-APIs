using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._2.Queries;

public class InformatieObjectTypenQueryParametersValidator : ZGWValidator<GetAllInformatieObjectTypenQueryParameters>
{
    public InformatieObjectTypenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Catalogus).IsUri();
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ConceptStatus));
        CascadeRuleFor(p => p.DatumGeldigheid).IsDate(required: false);
    }
}
