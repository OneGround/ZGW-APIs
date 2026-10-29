using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7;

public class ReserveZaakIdentificatieRequestValidator : ZGWValidator<ReserveZaakIdentificatieRequestDto>
{
    public ReserveZaakIdentificatieRequestValidator()
    {
        CascadeRuleFor(r => r.Bronorganisatie).IsRsin(true);
        // Note: the upper limit is configurable and is checked by the handler
        CascadeRuleFor(r => r.Aantal).GreaterThanOrEqualTo(1).When(r => r.Aantal.HasValue);
    }
}
