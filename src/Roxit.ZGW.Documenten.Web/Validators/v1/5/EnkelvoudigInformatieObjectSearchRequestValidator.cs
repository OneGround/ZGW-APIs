using FluentValidation;
using Microsoft.Extensions.Configuration;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1._5.Requests;
using Roxit.ZGW.Documenten.Web.Configuration;
using Roxit.ZGW.Documenten.Web.Validators.v1._5.Queries;

namespace Roxit.ZGW.Documenten.Web.Validators.v1._5;

public class EnkelvoudigInformatieObjectSearchRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectSearchRequestDto>
{
    public EnkelvoudigInformatieObjectSearchRequestValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(SupportedExpands.GetAll("enkelvoudiginformatieobject"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.Search);

        CascadeRuleFor(z => z.Uuid_In).NotNull();

        CascadeRuleForEach(z => z.Uuid_In).NotNull().NotEmpty().IsGuid();
    }
}
