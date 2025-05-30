using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;

public class VerblijfsadresValidator : ZGWValidator<Zaken.Contracts.v1.VerblijfsadresDto>
{
    public VerblijfsadresValidator()
    {
        CascadeRuleFor(a => a.AoaIdentificatie).NotNull().MaximumLength(100);
        CascadeRuleFor(a => a.WplWoonplaatsNaam).NotNull().MaximumLength(80);
        CascadeRuleFor(a => a.GorOpenbareRuimteNaam).NotNull().MaximumLength(80);
        CascadeRuleFor(a => a.AoaPostcode).MaximumLength(7);
        CascadeRuleFor(a => a.AoaHuisnummer).NotNull().InclusiveBetween(0, 99999);
        CascadeRuleFor(a => a.AoaHuisletter).MaximumLength(1);
        CascadeRuleFor(a => a.AoaHuisnummertoevoeging).MaximumLength(4);
        CascadeRuleFor(a => a.InpLocatiebeschrijving).MaximumLength(1000);
    }
}
