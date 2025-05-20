using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakVerzoekenQueryParametersValidator : ZGWValidator<GetAllZaakVerzoekenQueryParameters>
{
    public ZaakVerzoekenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Verzoek).IsUri();
    }
}
