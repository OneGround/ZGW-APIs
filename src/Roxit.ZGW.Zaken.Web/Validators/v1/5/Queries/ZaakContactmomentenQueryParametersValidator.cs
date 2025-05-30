using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Queries;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakContactmomentenQueryParametersValidator : ZGWValidator<GetAllZaakContactmomentenQueryParameters>
{
    public ZaakContactmomentenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Contactmoment).IsUri();
    }
}
