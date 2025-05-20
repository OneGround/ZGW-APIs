using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1.Queries;

public class EnkelvoudigInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllEnkelvoudigInformatieObjectenQueryParameters>
{
    public EnkelvoudigInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Bronorganisatie).IsRsin(required: false);
    }
}
