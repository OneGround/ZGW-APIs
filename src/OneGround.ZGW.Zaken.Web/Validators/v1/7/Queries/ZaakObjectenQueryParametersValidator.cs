using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

// Note: Mirrors v1._5.Queries.ZaakObjectenQueryParametersValidator (the one targeting the old,
// unversioned type) for the new v1._7 concrete type. "expand" is not validated here -- for v1.7,
// expand is validated by the new ExpandValidator<T> in the controller instead (see
// Controllers/v1/7/ZaakObjectenController.cs).
public class ZaakObjectenQueryParametersValidator : ZGWValidator<GetAllZaakObjectenQueryParameters>
{
    public ZaakObjectenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Object).IsUri();
        CascadeRuleFor(p => p.ObjectType).IsEnumName(typeof(ObjectType));
    }
}
