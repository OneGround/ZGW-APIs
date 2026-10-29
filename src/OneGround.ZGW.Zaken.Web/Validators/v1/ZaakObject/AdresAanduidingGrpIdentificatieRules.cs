using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

/// <summary>
/// The rules of the identificatie of the adresAanduidingGrp, shared by every version: oaoIdentificatie is the misspelled (deprecated) name of
/// aoaIdentificatie (#2576). One of the two is required, and a name that is sent is never blank, also not next to the other (as before). Without
/// either, both names are reported, so a client that already uses the correct name is not only told about the deprecated one.
/// </summary>
public static class AdresAanduidingGrpIdentificatieRules
{
    public static void Add(InlineValidator<AdresAanduidingGrpDto> v)
    {
#pragma warning disable CS0618 // Note: oaoIdentificatie is deprecated, but still accepted
        v.CascadeRuleFor(a => a.OaoIdentificatie).NotNull().When(a => a.AoaIdentificatie == null);
        v.CascadeRuleFor(a => a.OaoIdentificatie).NotEmpty().When(a => a.OaoIdentificatie != null);
        v.CascadeRuleFor(a => a.OaoIdentificatie).MaximumLength(100);
        v.CascadeRuleFor(a => a.AoaIdentificatie).NotNull().When(a => a.AoaIdentificatie == null && a.OaoIdentificatie == null);
        v.CascadeRuleFor(a => a.AoaIdentificatie).NotEmpty().MaximumLength(100).When(a => a.AoaIdentificatie != null);
#pragma warning restore CS0618
    }
}
