using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakRol;

public class VestigingZaakRolRequestValidator : ZGWValidator<VestigingZaakRolRequestDto>
{
    public VestigingZaakRolRequestValidator()
    {
        Include(new ZaakRolRequestDtoValidator());
        CascadeRuleFor(z => z.BetrokkeneIdentificatie)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(b => b.VestigingsNummer).MaximumLength(24);
                v.CascadeRuleFor(b => b.Verblijfsadres).SetValidator(new VerblijfsadresValidator());
                v.CascadeRuleFor(b => b.SubVerblijfBuitenland).SetValidator(new SubVerblijfBuitenlandValidator());
            });
    }
}
