using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakRol;

public class VerblijfsadresValidator : ZGWValidator<VerblijfsadresDto>
{
    public VerblijfsadresValidator()
    {
        CascadeRuleFor(a => a.AoaIdentificatie).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(a => a.WplWoonplaatsNaam).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(a => a.GorOpenbareRuimteNaam).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(a => a.AoaPostcode).MaximumLength(7);
        CascadeRuleFor(a => a.AoaHuisnummer).NotNull().InclusiveBetween(0, 99999);
        CascadeRuleFor(a => a.AoaHuisletter).MaximumLength(1);
        CascadeRuleFor(a => a.AoaHuisnummertoevoeging).MaximumLength(4);
        CascadeRuleFor(a => a.InpLocatiebeschrijving).MaximumLength(1000);
    }
}
