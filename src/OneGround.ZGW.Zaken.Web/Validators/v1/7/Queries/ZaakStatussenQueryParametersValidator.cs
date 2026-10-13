using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

// Note: Mirrors v1._5.Queries.ZaakStatussenQueryParametersValidator for the shared filter fields.
// Unlike v1._5, "expand" is not validated here -- for v1.7, expand is validated by the new
// ExpandValidator<T> in the controller instead (see Controllers/v1/7/ZaakStatussenController.cs).
public class ZaakStatussenQueryParametersValidator : ZGWValidator<GetAllZaakStatussenQueryParameters>
{
    public ZaakStatussenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.StatusType).IsUri();
        CascadeRuleFor(p => p.IndicatieLaatstGezetteStatus).IsBoolean();
    }
}
