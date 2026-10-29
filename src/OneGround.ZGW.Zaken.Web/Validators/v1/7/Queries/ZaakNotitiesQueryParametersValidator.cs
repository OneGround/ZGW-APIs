using FluentValidation;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Queries;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7.Queries;

public class ZaakNotitiesQueryParametersValidator : ZGWValidator<GetAllZaakNotitiesQueryParameters>
{
    public ZaakNotitiesQueryParametersValidator()
    {
        CascadeRuleFor(p => p.GerelateerdAan).IsUri();
        CascadeRuleFor(p => p.NotitieType).IsEnumName(typeof(ZaakNotitieType));
        CascadeRuleFor(p => p.Status).IsEnumName(typeof(ZaakNotitieStatus));

        CascadeRuleFor(p => p.Aanmaakdatum).IsDateTime();
        CascadeRuleFor(p => p.Aanmaakdatum__gt).IsDateTime();
        CascadeRuleFor(p => p.Aanmaakdatum__gte).IsDateTime();
        CascadeRuleFor(p => p.Aanmaakdatum__lt).IsDateTime();
        CascadeRuleFor(p => p.Aanmaakdatum__lte).IsDateTime();
        CascadeRuleFor(p => p.Wijzigingsdatum).IsDateTime();
        CascadeRuleFor(p => p.Wijzigingsdatum__gt).IsDateTime();
        CascadeRuleFor(p => p.Wijzigingsdatum__gte).IsDateTime();
        CascadeRuleFor(p => p.Wijzigingsdatum__lt).IsDateTime();
        CascadeRuleFor(p => p.Wijzigingsdatum__lte).IsDateTime();

        CascadeRuleFor(p => p.PageSize)
            .Must(value => value == null || (int.TryParse(value, out var pageSize) && pageSize >= 1))
            .WithMessage("Het aantal resultaten per pagina moet een geheel getal van minimaal 1 zijn.")
            .WithErrorCode(ErrorCode.Invalid);
    }
}
