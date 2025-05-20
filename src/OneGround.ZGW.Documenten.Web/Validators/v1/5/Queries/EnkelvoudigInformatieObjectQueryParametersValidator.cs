using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Web.Configuration;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._5.Queries;

public class EnkelvoudigInformatieObjectQueryParametersValidator : ZGWValidator<GetEnkelvoudigInformatieObjectQueryParameters>
{
    public EnkelvoudigInformatieObjectQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll("enkelvoudiginformatieobject"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.Get);

        CascadeRuleFor(p => p.Versie).IsInteger();
        CascadeRuleFor(p => p.RegistratieOp).IsDateTime();
    }
}
