using FluentValidation;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.Queries;

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
