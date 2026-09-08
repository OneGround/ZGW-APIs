using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._7.Queries;

public class ObjectInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllObjectInformatieObjectenQueryParameters>
{
    public ObjectInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Object).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
