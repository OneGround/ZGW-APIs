using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Besluiten.Web.Validators.Queries;

public class BesluitInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllBesluitInformatieObjectenQueryParameters>
{
    public BesluitInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Besluit).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
