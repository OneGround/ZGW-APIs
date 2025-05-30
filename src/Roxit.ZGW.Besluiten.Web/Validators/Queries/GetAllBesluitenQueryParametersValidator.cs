using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.Web.Configuration;
using Roxit.ZGW.Besluiten.Web.Expands;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Besluiten.Web.Validators.Queries;

public class GetAllBesluitenQueryParametersValidator : ZGWValidator<GetAllBesluitenQueryParameters>
{
    public GetAllBesluitenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll(ExpanderNames.BesluitExpander))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.List);
    }
}
