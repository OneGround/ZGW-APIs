using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;

public class MedewerkerZaakRolRequestValidator : ZGWValidator<MedewerkerZaakRolRequestDto>
{
    public MedewerkerZaakRolRequestValidator()
    {
        Include(new ZaakRolRequestDtoValidator());
        CascadeRuleFor(z => z.BetrokkeneIdentificatie)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(b => b.Identificatie).MaximumLength(24);
                v.CascadeRuleFor(b => b.Achternaam).MaximumLength(200);
                v.CascadeRuleFor(b => b.Voorletters).MaximumLength(20);
                v.CascadeRuleFor(b => b.VoorvoegselAchternaam).MaximumLength(10);
            });
    }
}
