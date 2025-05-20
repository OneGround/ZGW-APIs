using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakStatussenQueryParametersValidator : ZGWValidator<GetAllZaakStatussenQueryParameters>
{
    public ZaakStatussenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.StatusType).IsUri();
        CascadeRuleFor(p => p.IndicatieLaatstGezetteStatus).IsBoolean();
    }
}
