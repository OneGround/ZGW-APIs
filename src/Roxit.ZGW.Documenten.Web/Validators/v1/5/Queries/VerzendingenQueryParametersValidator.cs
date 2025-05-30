using FluentValidation;
using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1._5.Queries;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Configuration;

namespace Roxit.ZGW.Documenten.Web.Validators.v1._5.Queries;

public class VerzendingenQueryParametersValidator : ZGWValidator<GetAllVerzendingenQueryParameters>
{
    public VerzendingenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll("verzending"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.List);

        CascadeRuleFor(v => v.AardRelatie).IsEnumName(typeof(AardRelatie)).When(v => v.AardRelatie != null);
        CascadeRuleFor(p => p.InformatieObject).IsUri();
        CascadeRuleFor(p => p.Betrokkene).IsUri();
    }
}
