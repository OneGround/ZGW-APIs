using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakRol;

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
