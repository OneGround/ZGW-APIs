using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1._5.Queries;
using Roxit.ZGW.Documenten.Web.Configuration;

namespace Roxit.ZGW.Documenten.Web.Validators.v1._5.Queries;

public class VerzendingQueryParametersValidator : ZGWValidator<GetVerzendingQueryParameters>
{
    public VerzendingQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll("verzending"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.Get);
    }
}
