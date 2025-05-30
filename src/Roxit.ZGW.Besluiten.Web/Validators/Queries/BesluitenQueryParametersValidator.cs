using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Besluiten.Web.Validators.Queries;

public class BesluitenQueryParametersValidator : ZGWValidator<GetAllBesluitenQueryParameters>
{
    public BesluitenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.VerantwoordelijkeOrganisatie).IsRsin(required: false);
        CascadeRuleFor(p => p.BesluitType).IsUri();
        CascadeRuleFor(p => p.Zaak).IsUri();
    }
}
