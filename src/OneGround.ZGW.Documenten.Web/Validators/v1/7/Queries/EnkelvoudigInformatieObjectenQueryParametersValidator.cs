using FluentValidation;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.Web.Configuration;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._7.Queries;

public class EnkelvoudigInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllEnkelvoudigInformatieObjectenQueryParameters>
{
    public EnkelvoudigInformatieObjectenQueryParametersValidator(IConfiguration configuration)
    {
        var applicationConfiguration = configuration.GetSection("Application").Get<ApplicationConfiguration>();

        CascadeRuleFor(p => p.Expand)
            .ExpandsValid(_5.Queries.SupportedExpands.GetAll("enkelvoudiginformatieobject"))
            .IsExpandEnabled(applicationConfiguration.ExpandSettings.List);

        CascadeRuleFor(p => p.Bronorganisatie).IsRsin(required: false);
        CascadeRuleFor(p => p.ObjectInformatieObjecten_Object).IsUri();
        CascadeRuleFor(p => p.ObjectInformatieObjecten_ObjectType)
            .IsEnumName(typeof(DataModel.ObjectType), caseSensitive: false)
            .When(p => !string.IsNullOrEmpty(p.ObjectInformatieObjecten_ObjectType));
    }
}
