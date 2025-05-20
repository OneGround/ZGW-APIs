using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Web.Configuration;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._5.Queries;

public class GebruiksRechtenQueryParametersValidator : ZGWValidator<GetAllGebruiksRechtenQueryParameters>
{
    public GebruiksRechtenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll("gebruiksrecht"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.List);

        CascadeRuleFor(p => p.InformatieObject).IsUri();
        CascadeRuleFor(p => p.Startdatum__lt).IsDateTime();
        CascadeRuleFor(p => p.Startdatum__lte).IsDateTime();
        CascadeRuleFor(p => p.Startdatum__gt).IsDateTime();
        CascadeRuleFor(p => p.Startdatum__gte).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__lt).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__lte).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__gt).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__gte).IsDateTime();
    }
}
