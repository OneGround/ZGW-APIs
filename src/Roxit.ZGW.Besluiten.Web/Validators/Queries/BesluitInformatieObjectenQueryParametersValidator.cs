using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Besluiten.Web.Validators.Queries;

public class BesluitInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllBesluitInformatieObjectenQueryParameters>
{
    public BesluitInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Besluit).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
