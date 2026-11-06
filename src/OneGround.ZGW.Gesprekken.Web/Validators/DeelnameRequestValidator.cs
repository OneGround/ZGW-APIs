using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Gesprekken.Contracts.v1.Requests;
using OneGround.ZGW.Gesprekken.Web.Helpers;

namespace OneGround.ZGW.Gesprekken.Web.Validators;

public class DeelnameRequestValidator : ZGWValidator<DeelnameRequestDto>
{
    public DeelnameRequestValidator()
    {
        CascadeRuleFor(d => d.Type).NotNull().NotEmpty().Must(t => t == "actor" || t == "partij").WithMessage("Kies 'actor' of 'partij'.");
        CascadeRuleFor(d => d.Partij).NotNull().NotEmpty().IsUri().MaximumLength(1000).When(d => d.Type == "partij");
        CascadeRuleFor(d => d.Actor).NotNull().NotEmpty().IsUri().MaximumLength(1000).When(d => d.Type == "actor");
        CascadeRuleFor(d => d.Naam).NotNull().NotEmpty().MaximumLength(200);
        CascadeRuleFor(d => d.Namens).IsUri().MaximumLength(1000);
    }
}

public class GespreksdeelnameRequestValidator : ZGWValidator<GespreksdeelnameRequestDto>
{
    public GespreksdeelnameRequestValidator()
    {
        Include(new DeelnameRequestValidator());
        CascadeRuleFor(d => d.Gesprek).NotNull().NotEmpty().IsGrcUrl(GrcUrl.Gesprekken);
    }
}
