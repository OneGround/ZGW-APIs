using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Common.Web.Versioning;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakRol;

// Note: This validator serves API versions 1.5 and 1.7 (the v1 types have their own validator). The identificatie of the medewerker may hold
// 24 characters up to version 1.5; from version 1.7 it may hold 254.
public class MedewerkerZaakRolRequestValidator : ZGWValidator<MedewerkerZaakRolRequestDto>
{
    private const int MaxIdentificatieLength = 24;
    private const int MaxIdentificatieLengthFrom1_7 = 254;

    public MedewerkerZaakRolRequestValidator(IHttpContextAccessor httpContextAccessor)
    {
        Include(new ZaakRolRequestDtoValidator());
        CascadeRuleFor(z => z.BetrokkeneIdentificatie)
            .ChildRules(v =>
            {
                // Note: Two rules with a condition instead of one rule with a computed maximum, so the standard length validator (and its message
                // and error code) is used for both. The condition is evaluated per request, as the validator itself is a singleton.
                v.CascadeRuleFor(b => b.Identificatie).MaximumLength(MaxIdentificatieLength).When(_ => !IsVersion1_7OrHigher(httpContextAccessor));
                v.CascadeRuleFor(b => b.Identificatie)
                    .MaximumLength(MaxIdentificatieLengthFrom1_7)
                    .When(_ => IsVersion1_7OrHigher(httpContextAccessor));
                v.CascadeRuleFor(b => b.Achternaam).MaximumLength(200);
                v.CascadeRuleFor(b => b.Voorletters).MaximumLength(20);
                v.CascadeRuleFor(b => b.VoorvoegselAchternaam).MaximumLength(10);
            });
    }

    private static bool IsVersion1_7OrHigher(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.GetRequestedZgwApiVersion() >= new ApiVersion(1, 7);
    }
}
