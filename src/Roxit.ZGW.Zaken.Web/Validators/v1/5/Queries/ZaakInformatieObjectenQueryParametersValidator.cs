using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllZaakInformatieObjectenQueryParameters>
{
    public ZaakInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
