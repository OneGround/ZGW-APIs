using FluentValidation;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._7.Queries;

public class EnkelvoudigInformatieObjectenQueryParametersValidator : ZGWValidator<GetAllEnkelvoudigInformatieObjectenQueryParameters>
{
    public EnkelvoudigInformatieObjectenQueryParametersValidator(IConfiguration configuration)
    {
        CascadeRuleFor(p => p.Bronorganisatie).IsRsin(required: false);
        CascadeRuleFor(p => p.ObjectInformatieObjecten_Object).IsUri();
        CascadeRuleFor(p => p.ObjectInformatieObjecten_ObjectType)
            .IsEnumName(typeof(DataModel.ObjectType), caseSensitive: false)
            .When(p => !string.IsNullOrEmpty(p.ObjectInformatieObjecten_ObjectType));
    }
}
