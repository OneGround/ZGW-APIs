using FluentValidation;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1.Queries;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Validators.v1.Queries;

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
