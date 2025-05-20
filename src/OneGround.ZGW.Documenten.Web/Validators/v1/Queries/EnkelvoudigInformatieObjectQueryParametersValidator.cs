using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1.Queries;

public class EnkelvoudigInformatieObjectQueryParametersValidator : ZGWValidator<GetEnkelvoudigInformatieObjectQueryParameters>
{
    public EnkelvoudigInformatieObjectQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Versie).IsInteger();
        CascadeRuleFor(p => p.RegistratieOp).IsDateTime();
    }
}
