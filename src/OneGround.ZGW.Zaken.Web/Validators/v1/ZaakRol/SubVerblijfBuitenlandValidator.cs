using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakRol;

public class SubVerblijfBuitenlandValidator : ZGWValidator<SubVerblijfBuitenlandDto>
{
    public SubVerblijfBuitenlandValidator()
    {
        CascadeRuleFor(a => a.LndLandcode).NotNull().NotEmpty().MaximumLength(4);
        CascadeRuleFor(a => a.LndLandnaam).NotNull().NotEmpty().MaximumLength(40);
        CascadeRuleFor(a => a.SubAdresBuitenland1).MaximumLength(35);
        CascadeRuleFor(a => a.SubAdresBuitenland2).MaximumLength(35);
        CascadeRuleFor(a => a.SubAdresBuitenland3).MaximumLength(35);
    }
}
