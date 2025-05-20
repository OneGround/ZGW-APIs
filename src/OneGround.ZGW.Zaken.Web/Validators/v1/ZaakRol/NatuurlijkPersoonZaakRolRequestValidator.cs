using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakRol;

public class NatuurlijkPersoonZaakRolRequestValidator : ZGWValidator<NatuurlijkPersoonZaakRolRequestDto>
{
    public NatuurlijkPersoonZaakRolRequestValidator()
    {
        Include(new ZaakRolRequestDtoValidator());
        CascadeRuleFor(z => z.BetrokkeneIdentificatie)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(b => b.InpBsn).MaximumLength(9);
                v.CascadeRuleFor(b => b.AnpIdentificatie).MaximumLength(17);
                v.CascadeRuleFor(b => b.InpANummer).Matches(@"^[1-9][0-9]{9}$");
                v.CascadeRuleFor(b => b.Geslachtsnaam).MaximumLength(200);
                v.CascadeRuleFor(b => b.VoorvoegselGeslachtsnaam).MaximumLength(80);
                v.CascadeRuleFor(b => b.Voorletters).MaximumLength(20);
                v.CascadeRuleFor(b => b.Voornamen).MaximumLength(200);
                v.CascadeRuleFor(b => b.Geslachtsaanduiding).IsEnumName(typeof(Geslachtsaanduiding));
                v.CascadeRuleFor(b => b.Geboortedatum).IsDate(false);
                v.CascadeRuleFor(b => b.Verblijfsadres).SetValidator(new VerblijfsadresValidator());
                v.CascadeRuleFor(b => b.SubVerblijfBuitenland).SetValidator(new SubVerblijfBuitenlandValidator());
            });
    }
}
