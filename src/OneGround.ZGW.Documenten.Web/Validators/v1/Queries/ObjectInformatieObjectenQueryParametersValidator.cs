using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1.Queries;

public class ObjectInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllObjectInformatieObjectenQueryParameters>
{
    public ObjectInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Object).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
