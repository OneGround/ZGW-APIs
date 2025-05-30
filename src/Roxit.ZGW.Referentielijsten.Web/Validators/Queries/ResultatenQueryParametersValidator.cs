using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;

namespace Roxit.ZGW.Referentielijsten.Web.Validators.Queries;

public class ResultatenQueryParametersValidator : ZGWValidator<GetAllResultatenQueryParameters>
{
    public ResultatenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ProcesType).IsUri();
    }
}
