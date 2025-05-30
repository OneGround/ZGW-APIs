using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;

namespace Roxit.ZGW.Documenten.Web.Validators.v1.Queries;

public class EnkelvoudigInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllEnkelvoudigInformatieObjectenQueryParameters>
{
    public EnkelvoudigInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Bronorganisatie).IsRsin(required: false);
    }
}
