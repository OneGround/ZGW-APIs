using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakObjectenQueryParametersValidator : ZGWValidator<GetAllZaakObjectenQueryParameters>
{
    public ZaakObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Object).IsUri();
        CascadeRuleFor(p => p.ObjectType).IsEnumName(typeof(ObjectType));
    }
}
