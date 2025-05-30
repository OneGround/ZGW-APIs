using FluentValidation;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Documenten.Contracts.v1.Requests;

namespace Roxit.ZGW.Documenten.Web.Validators.v1;

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
