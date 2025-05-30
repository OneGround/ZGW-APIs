using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;

namespace Roxit.ZGW.Documenten.Web.Validators.v1.Queries;

public class EnkelvoudigInformatieObjectQueryParametersValidator : ZGWValidator<GetEnkelvoudigInformatieObjectQueryParameters>
{
    public EnkelvoudigInformatieObjectQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Versie).IsInteger();
        CascadeRuleFor(p => p.RegistratieOp).IsDateTime();
    }
}
