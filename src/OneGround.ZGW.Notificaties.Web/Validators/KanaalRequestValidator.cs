using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;

namespace OneGround.ZGW.Notificaties.Web.Validators;

public class KanaalRequestValidator : ZGWValidator<KanaalRequestDto>
{
    public KanaalRequestValidator()
    {
        CascadeRuleFor(r => r.Naam).NotNull().NotEmpty().MaximumLength(50);
        CascadeRuleFor(r => r.DocumentatieLink).IsUri().MaximumLength(200);
        CascadeRuleForEach(r => r.Filters).NotNull().NotEmpty().MaximumLength(200);
    }
}
