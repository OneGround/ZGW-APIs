using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Gesprekken.Contracts.v1.Queries;
using OneGround.ZGW.Gesprekken.Web.Helpers;

namespace OneGround.ZGW.Gesprekken.Web.Validators.Queries;

public class GetAllGesprekkenQueryParametersValidator : ZGWValidator<GetAllGesprekkenQueryParameters>
{
    public GetAllGesprekkenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.AanleidingZaak).IsUri();
        CascadeRuleFor(p => p.AanleidingPlan).IsUri();
        CascadeRuleFor(p => p.AanvangsmomentGesprekGte).IsDateTime();
        CascadeRuleFor(p => p.AanvangsmomentGesprekLte).IsDateTime();
        CascadeRuleFor(p => p.GespreksdeelnamePartij).IsUri();
        CascadeRuleFor(p => p.GespreksdeelnameActor).IsUri();
        CascadeRuleFor(p => p.Gespreksdeelname).IsGrcUrl(GrcUrl.Gespreksdeelnames);
        CascadeRuleFor(p => p.GespreksdeelnameType).Must(t => t == null || t == "actor" || t == "partij").WithMessage("Kies 'actor' of 'partij'.");
        CascadeRuleFor(p => p.PageSize)
            .Must(s => s == null || (int.TryParse(s, out var size) && size is >= 1 and <= 100))
            .WithMessage("Geef een getal van 1 tot en met 100.");
    }
}
