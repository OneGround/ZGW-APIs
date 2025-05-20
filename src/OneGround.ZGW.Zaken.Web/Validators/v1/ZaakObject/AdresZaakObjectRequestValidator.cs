using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class AdresZaakObjectRequestValidator : ZGWValidator<AdresZaakObjectRequestDto>
{
    public AdresZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new AdresZaakObjectDtoValidator());
    }
}

public class AdresZaakObjectDtoValidator : ZGWValidator<AdresZaakObjectDto>
{
    public AdresZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.Identificatie).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(o => o.WplWoonplaatsNaam).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(o => o.GorOpenbareRuimteNaam).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(o => o.Huisletter).MaximumLength(1);
        CascadeRuleFor(o => o.HuisnummerToevoeging).MaximumLength(4);
        CascadeRuleFor(o => o.Postcode).MaximumLength(7);
    }
}
