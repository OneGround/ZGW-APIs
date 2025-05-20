using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1.Queries;

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
