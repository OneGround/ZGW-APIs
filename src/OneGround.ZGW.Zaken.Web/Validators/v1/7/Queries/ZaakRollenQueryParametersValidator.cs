using FluentValidation;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

// Note: Mirrors v1.Queries.ZaakRollenQueryParametersValidator (the one targeting the old,
// unversioned type) for the new v1._7 concrete type. "expand" is not validated here -- for v1.7,
// expand is validated by the new ExpandValidator<T> in the controller instead (see
// Controllers/v1/7/ZaakRollenController.cs).
public class ZaakRollenQueryParametersValidator : ZGWValidator<GetAllZaakRollenQueryParameters>
{
    public ZaakRollenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Zaak).IsUri();
        CascadeRuleFor(p => p.Betrokkene).IsUri();
        CascadeRuleFor(p => p.RolType).IsUri();
        CascadeRuleFor(p => p.BetrokkeneType).IsEnumName(typeof(BetrokkeneType));
        CascadeRuleFor(p => p.OmschrijvingGeneriek).IsEnumName(typeof(OmschrijvingGeneriek));
    }
}
