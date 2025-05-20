using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Notificaties.Contracts.v1;

namespace OneGround.ZGW.Notificaties.Web.Validators;

public class NotificatieRequestValidator : ZGWValidator<NotificatieDto>
{
    public NotificatieRequestValidator()
    {
        CascadeRuleFor(r => r.Kanaal).NotNull().NotEmpty().MaximumLength(50);
        CascadeRuleFor(r => r.HoofdObject).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(r => r.Resource).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(r => r.ResourceUrl).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(r => r.Actie).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(r => r.Aanmaakdatum).NotNull().NotEmpty().IsDateTime();
    }
}
