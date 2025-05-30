using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Validators.v1.ZaakRol;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;

public class NietNatuurlijkPersoonZaakRolRequestValidator : ZGWValidator<NietNatuurlijkPersoonZaakRolRequestDto>
{
    public NietNatuurlijkPersoonZaakRolRequestValidator()
    {
        Include(new ZaakRolRequestDtoValidator());
        CascadeRuleFor(z => z.BetrokkeneIdentificatie)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(b => b.InnNnpId).MaximumLength(9);
                v.CascadeRuleFor(b => b.AnnIdentificatie).MaximumLength(17);
                v.CascadeRuleFor(b => b.StatutaireNaam).MaximumLength(500);
                v.CascadeRuleFor(b => b.InnRechtsvorm).IsEnumName(typeof(InnRechtsvorm));
                v.CascadeRuleFor(b => b.Bezoekadres).MaximumLength(1000);
                v.CascadeRuleFor(b => b.SubVerblijfBuitenland).SetValidator(new SubVerblijfBuitenlandValidator());
            });
    }
}
