using FluentValidation;
using Microsoft.Extensions.Configuration;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._7.Queries;

public class VerzendingenQueryParametersValidator : ZGWValidator<GetAllVerzendingenQueryParameters>
{
    public VerzendingenQueryParametersValidator(IConfiguration configuration)
    {
        CascadeRuleFor(v => v.AardRelatie).IsEnumName(typeof(AardRelatie)).When(v => v.AardRelatie != null);
        CascadeRuleFor(p => p.InformatieObject).IsUri();
        CascadeRuleFor(p => p.Betrokkene).IsUri();
    }
}
