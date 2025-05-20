using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

public class ZaakSearchRequestValidator : ZGWValidator<ZaakSearchRequestDto>
{
    public ZaakSearchRequestValidator()
    {
        CascadeRuleFor(r => r.ZaakGeometry)
            .NotNull()
            .ChildRules(v =>
            {
                v.CascadeRuleFor(z => z.Within).NotNull();
            });

        Include(new ZakenSearchValidator());
    }
}
