using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Notificaties.Contracts.v1.Requests;

namespace Roxit.ZGW.Notificaties.Web.Validators;

public class KanaalRequestValidator : ZGWValidator<KanaalRequestDto>
{
    public KanaalRequestValidator()
    {
        CascadeRuleFor(r => r.Naam).NotNull().NotEmpty().MaximumLength(50);
        CascadeRuleFor(r => r.DocumentatieLink).IsUri().MaximumLength(200);
        CascadeRuleForEach(r => r.Filters).NotNull().NotEmpty().MaximumLength(200);
    }
}
