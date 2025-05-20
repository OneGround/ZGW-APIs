using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;

namespace OneGround.ZGW.Documenten.Web.Validators.v1.Queries;

public class GebruiksRechtenQueryParametersValidator : ZGWValidator<GetAllGebruiksRechtenQueryParameters>
{
    public GebruiksRechtenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.InformatieObject).IsUri();
        CascadeRuleFor(p => p.Startdatum__lt).IsDateTime();
        CascadeRuleFor(p => p.Startdatum__lte).IsDateTime();
        CascadeRuleFor(p => p.Startdatum__gt).IsDateTime();
        CascadeRuleFor(p => p.Startdatum__gte).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__lt).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__lte).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__gt).IsDateTime();
        CascadeRuleFor(p => p.Einddatum__gte).IsDateTime();
    }
}
