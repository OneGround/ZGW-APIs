using FluentValidation;
using Roxit.ZGW.Besluiten.Contracts.v1.Requests;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Besluiten.Web.Validators;

public class BesluitRequestValidator : ZGWValidator<BesluitRequestDto>
{
    public BesluitRequestValidator()
    {
        CascadeRuleFor(z => z.Identificatie).MaximumLength(50);
        CascadeRuleFor(z => z.VerantwoordelijkeOrganisatie).IsRsin(true);
        CascadeRuleFor(z => z.BesluitType).NotNull().NotEmpty().IsUri().MaximumLength(200);
        CascadeRuleFor(z => z.Zaak).IsUri().MaximumLength(200);
        CascadeRuleFor(z => z.Datum).IsDate(true);
        CascadeRuleFor(z => z.BestuursOrgaan).MaximumLength(50);
        CascadeRuleFor(z => z.IngangsDatum).IsDate(true);
        CascadeRuleFor(z => z.VervalDatum).IsDate(false);
        CascadeRuleFor(z => z.VervalReden).NotNull().IsEnumName(typeof(VervalReden)).When(z => z.VervalReden != string.Empty);
        CascadeRuleFor(z => z.PublicatieDatum).IsDate(false);
        CascadeRuleFor(z => z.VerzendDatum).IsDate(false);
        CascadeRuleFor(z => z.UiterlijkeReactieDatum).IsDate(false);
    }
}
