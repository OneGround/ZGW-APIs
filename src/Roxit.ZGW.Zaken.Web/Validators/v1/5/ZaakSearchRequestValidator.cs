using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5;

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
