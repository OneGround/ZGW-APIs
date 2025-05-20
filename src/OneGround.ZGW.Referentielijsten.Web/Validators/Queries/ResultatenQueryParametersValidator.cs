using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Referentielijsten.Web.Contracts.v1.Requests.Queries;

namespace OneGround.ZGW.Referentielijsten.Web.Validators.Queries;

public class ResultatenQueryParametersValidator : ZGWValidator<GetAllResultatenQueryParameters>
{
    public ResultatenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.ProcesType).IsUri();
    }
}
