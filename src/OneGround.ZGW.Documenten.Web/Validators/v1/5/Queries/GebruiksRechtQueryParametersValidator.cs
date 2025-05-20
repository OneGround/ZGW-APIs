using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Web.Configuration;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._5.Queries;

public class GebruiksRechtQueryParametersValidator : ZGWValidator<GetGebruiksRechtQueryParameters>
{
    public GebruiksRechtQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll("gebruiksrecht"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.Get);
    }
}
