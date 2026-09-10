using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1._7.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1._7.Queries;

public class EnkelvoudigInformatieObjectQueryParametersValidator : ZGWValidator<GetEnkelvoudigInformatieObjectQueryParameters>
{
    public EnkelvoudigInformatieObjectQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Versie).IsInteger();
        CascadeRuleFor(p => p.RegistratieOp).IsDateTime();
    }
}
