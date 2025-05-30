using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;

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
