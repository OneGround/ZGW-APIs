using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;

namespace OneGround.ZGW.Documenten.Web.Validators.v1;

public class GebruiksRechtRequestValidator : ZGWValidator<GebruiksRechtRequestDto>
{
    public GebruiksRechtRequestValidator()
    {
        CascadeRuleFor(z => z.InformatieObject).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Startdatum).NotNull().NotEmpty().IsDateTime();
        CascadeRuleFor(z => z.Einddatum).IsDateTime();
        CascadeRuleFor(z => z.OmschrijvingVoorwaarden).NotNull().NotEmpty();
    }
}
