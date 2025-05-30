using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.Contracts.v1._5.Queries;
using Roxit.ZGW.Zaken.Web.Configuration;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.Queries;

public class SearchZakenQueryParametersValidator : ZGWValidator<SearchZakenQueryParameters>
{
    public SearchZakenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand).ExpandsValid(SupportedExpands.GetAll("zaak")).IsExpandEnabled(applicationConfiguration.ExpandSettings.Search);
    }
}
