using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllZaakInformatieObjectenQueryParameters>
{
    public ZaakInformatieObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.InformatieObject).IsUri();
    }
}
