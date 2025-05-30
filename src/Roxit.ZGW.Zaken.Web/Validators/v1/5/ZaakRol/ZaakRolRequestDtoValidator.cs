using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;

public class ZaakRolRequestDtoValidator : ZGWValidator<ZaakRolRequestDto>
{
    public ZaakRolRequestDtoValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Betrokkene).IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.BetrokkeneType).NotNull().NotEmpty().IsEnumName(typeof(BetrokkeneType));
        CascadeRuleFor(z => z.RolType).NotNull().NotEmpty().IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.RolToelichting).NotNull().NotEmpty().MaximumLength(1000);
        CascadeRuleFor(z => z.IndicatieMachtiging).IsEnumName(typeof(IndicatieMachtiging)).Unless(z => z.IndicatieMachtiging == "");
        CascadeRuleFor(z => z.AfwijkendeNaamBetrokkene).MaximumLength(625);
        CascadeRuleFor(z => z.ContactpersoonRol)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(b => b.EmailAdres).MaximumLength(254);
                v.CascadeRuleFor(b => b.Functie).MaximumLength(50);
                v.CascadeRuleFor(b => b.Telefoonnummer).MaximumLength(20);
                v.CascadeRuleFor(b => b.Naam).MaximumLength(40);
            });
    }
}
