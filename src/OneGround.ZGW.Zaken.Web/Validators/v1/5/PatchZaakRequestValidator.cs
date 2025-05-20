using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5;

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
            z => z.Processobject != null,
            () =>
            {
                CascadeRuleFor(z => z.Processobject.Datumkenmerk)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(250)
                    .OverridePropertyName("processobject.datumkenmerk");
                CascadeRuleFor(z => z.Processobject.Identificatie)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(250)
                    .OverridePropertyName("processobject.identificatie");
                CascadeRuleFor(z => z.Processobject.Objecttype)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(250)
                    .OverridePropertyName("processobject.objecttype");
                CascadeRuleFor(z => z.Processobject.Registratie)
                    .NotNull()
                    .NotEmpty()
                    .MaximumLength(250)
                    .OverridePropertyName("processobject.registratie");
            }
        );
    }
}
