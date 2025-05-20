using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using OneGround.ZGW.Zaken.Web.Configuration;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class ZakenQueryParametersValidator : ZGWValidator<GetAllZakenQueryParameters>
{
    public ZakenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand).ExpandsValid(SupportedExpands.GetAll("zaak")).IsExpandEnabled(applicationConfiguration.ExpandSettings.List);

        Include(new ZakenSearchValidator());
    }
}
