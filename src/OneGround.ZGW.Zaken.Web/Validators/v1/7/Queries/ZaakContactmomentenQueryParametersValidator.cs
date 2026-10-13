using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

// Note: Mirrors v1._5.Queries.ZaakContactmomentenQueryParametersValidator (the one targeting the old,
// unversioned type) for the new v1._7 concrete type. "expand" is not validated here -- for v1.7,
// expand is validated by the new ExpandValidator<T> in the controller instead (see
// Controllers/v1/7/ZaakContactmomentenController.cs).
public class ZaakContactmomentenQueryParametersValidator : ZGWValidator<GetAllZaakContactmomentenQueryParameters>
{
    public ZaakContactmomentenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Contactmoment).IsUri();
    }
}
