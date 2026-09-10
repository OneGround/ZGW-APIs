using FluentValidation;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._7.Requests;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._7;

public class EnkelvoudigInformatieObjectSearchRequestValidator : ZGWValidator<EnkelvoudigInformatieObjectSearchRequestDto>
{
    public EnkelvoudigInformatieObjectSearchRequestValidator(IConfiguration configuration)
    {
        CascadeRuleForEach(z => z.Uuid_In).IsGuid();

        CascadeRuleFor(p => p.Bronorganisatie).IsRsin(required: false);
        CascadeRuleFor(p => p.ObjectInformatieObjecten_Object).IsUri();
        CascadeRuleFor(p => p.ObjectInformatieObjecten_ObjectType)
            .IsEnumName(typeof(DataModel.ObjectType), caseSensitive: false)
            .When(p => !string.IsNullOrEmpty(p.ObjectInformatieObjecten_ObjectType));
    }
}
