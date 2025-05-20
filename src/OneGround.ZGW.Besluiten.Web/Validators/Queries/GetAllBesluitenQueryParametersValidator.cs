using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Besluiten.Contracts.v1.Queries;
using OneGround.ZGW.Besluiten.Web.Configuration;
using OneGround.ZGW.Besluiten.Web.Expands;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Besluiten.Web.Validators.Queries;

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
