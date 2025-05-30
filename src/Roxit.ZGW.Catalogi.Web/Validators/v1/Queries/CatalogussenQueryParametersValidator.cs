using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1.Queries;

public class CatalogussenQueryParametersValidator : ZGWValidator<GetAllCatalogussenQueryParameters>
{
    public CatalogussenQueryParametersValidator()
    {
        CascadeRuleFor(p => p.Domein).MaximumLength(5);
        CascadeRuleFor(p => p.Rsin).IsRsin(required: false);

        CascadeRuleForEach(p => TryList(p.Domein__in)).MaximumLength(5).WithName("domein__in");
        CascadeRuleForEach(p => TryList(p.Rsin__in)).IsRsin().WithName("rsin__in");
    }
}
