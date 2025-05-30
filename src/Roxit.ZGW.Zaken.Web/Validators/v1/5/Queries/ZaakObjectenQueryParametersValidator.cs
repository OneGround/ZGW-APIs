using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZaakObjectenQueryParametersValidator : ZGWValidator<GetAllZaakObjectenQueryParameters>
{
    public ZaakObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Object).IsUri();
        CascadeRuleFor(p => p.ObjectType).IsEnumName(typeof(ObjectType));
    }
}
