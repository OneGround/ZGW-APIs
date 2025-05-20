using FluentValidation;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.Web.Configuration;
using OneGround.ZGW.Documenten.Web.Validators.v1._5.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._5;

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
