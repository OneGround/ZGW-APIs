using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakRol;

public class OrganisatorischeEenheidZaakRolRequestValidator : ZGWValidator<OrganisatorischeEenheidZaakRolRequestDto>
{
    public OrganisatorischeEenheidZaakRolRequestValidator()
    {
        Include(new ZaakRolRequestDtoValidator());
        CascadeRuleFor(z => z.BetrokkeneIdentificatie)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(b => b.Identificatie).MaximumLength(24);
                v.CascadeRuleFor(b => b.Naam).MaximumLength(50);
                v.CascadeRuleFor(b => b.IsGehuisvestIn).MaximumLength(24);
            });
    }
}
