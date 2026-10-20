using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

#pragma warning disable CS0618 // Note: oaoIdentificatie is deprecated, but still accepted (#2576)

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class TerreinGebouwdObjectZaakObjectRequestValidator : ZGWValidator<TerreinGebouwdObjectZaakObjectRequestDto>
{
    public TerreinGebouwdObjectZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new TerreinGebouwdObjectZaakObjectDtoValidator());
    }
}

public class TerreinGebouwdObjectZaakObjectDtoValidator : ZGWValidator<TerreinGebouwdObjectZaakObjectDto>
{
    public TerreinGebouwdObjectZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.Identificatie).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(o => o.AdresAanduidingGrp)
            .ChildRules(v =>
            {
                v.CascadeRuleFor(a => a.NumIdentificatie).MaximumLength(100);
                // Note: oaoIdentificatie is the misspelled name of aoaIdentificatie (#2576). One of the two is required; aoaIdentificatie wins when set.
                v.CascadeRuleFor(a => a.OaoIdentificatie).NotNull().NotEmpty().When(a => a.AoaIdentificatie == null);
                v.CascadeRuleFor(a => a.OaoIdentificatie).MaximumLength(100);
                v.CascadeRuleFor(a => a.AoaIdentificatie).NotEmpty().MaximumLength(100).When(a => a.AoaIdentificatie != null);
                v.CascadeRuleFor(a => a.WplWoonplaatsNaam).NotNull().NotEmpty().MaximumLength(80);
                v.CascadeRuleFor(a => a.GorOpenbareRuimteNaam).NotNull().NotEmpty().MaximumLength(80);
                v.CascadeRuleFor(a => a.AoaPostcode).MaximumLength(7);
                v.CascadeRuleFor(a => a.AoaHuisletter).MaximumLength(1);
                v.CascadeRuleFor(a => a.AoaHuisnummertoevoeging).MaximumLength(4);
                v.CascadeRuleFor(a => a.OgoLocatieAanduiding).MaximumLength(100);
            });
    }
}
