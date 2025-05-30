using FluentValidation;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Web.Validations;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Validators.v1;

public class PatchZaakRequestValidator : ZGWValidator<PatchZaakValidationDto>
{
    public PatchZaakRequestValidator()
    {
        When(
            z => z.Verlenging != null,
            () =>
            {
                CascadeRuleFor(z => z.Verlenging.Duur).NotNull().NotEmpty().IsDuration().OverridePropertyName("verlenging.duur");
                CascadeRuleFor(z => z.Verlenging.Reden).NotNull().NotEmpty().MaximumLength(200).OverridePropertyName("verlenging.reden");
            }
        );

        When(
            z => z.Opschorting != null,
            () =>
            {
                CascadeRuleFor(z => z.Opschorting.Indicatie).NotNull().OverridePropertyName("opschorting.indicatie");
                CascadeRuleFor(z => z.Opschorting.Reden).NotNull().NotEmpty().MaximumLength(200).OverridePropertyName("opschorting.reden");
            }
        );

        When(
            z => z.Betalingsindicatie == "nvt",
            () =>
            {
                CascadeRuleFor(z => z.LaatsteBetaaldatum)
                    .Null()
                    .OverridePropertyName("laatsteBetaaldatum")
                    .WithMessage($"Datum mag niet gezet worden omdat de betalingsindicatie '{BetalingsIndicatie.nvt}' is.")
                    .WithErrorCode(ErrorCode.BetalingNvt);
            }
        );
    }
}
