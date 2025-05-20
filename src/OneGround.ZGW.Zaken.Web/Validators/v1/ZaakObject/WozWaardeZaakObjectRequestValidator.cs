using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class WozWaardeZaakObjectRequestValidator : ZGWValidator<WozWaardeZaakObjectRequestDto>
{
    public WozWaardeZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new WozWaardeZaakObjectDtoValidator());
    }
}

public class WozWaardeZaakObjectDtoValidator : ZGWValidator<WozWaardeZaakObjectDto>
{
    public WozWaardeZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.WaardePeildatum).IsDate(true);
        CascadeRuleFor(o => o.IsVoor)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(o => o.WozObjectNummer).NotNull().NotEmpty().MaximumLength(100);
                v.CascadeRuleFor(o => o.AanduidingWozObject)
                    .ChildRules(v =>
                    {
                        v.CascadeRuleFor(o => o.AoaIdentificatie).NotNull().NotEmpty().MaximumLength(100);
                        v.CascadeRuleFor(o => o.WplWoonplaatsNaam).NotNull().NotEmpty().MaximumLength(80);
                        v.CascadeRuleFor(o => o.GorOpenbareRuimteNaam).NotNull().NotEmpty().MaximumLength(80);
                        v.CascadeRuleFor(o => o.AoaPostcode).MaximumLength(7);
                        v.CascadeRuleFor(o => o.AoaHuisletter).MaximumLength(1);
                        v.CascadeRuleFor(o => o.AoaHuisnummerToevoeging).MaximumLength(4);
                        v.CascadeRuleFor(o => o.LocatieOmschrijving).MaximumLength(1000);
                    });
            });
    }
}
