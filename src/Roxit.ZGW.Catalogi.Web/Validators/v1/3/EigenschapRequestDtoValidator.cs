using FluentValidation;
using Roxit.ZGW.Catalogi.Contracts.v1._3.Requests;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Web.Validations;

namespace Roxit.ZGW.Catalogi.Web.Validators.v1._3;

public class EigenschapRequestDtoValidator : ZGWValidator<EigenschapRequestDto>
{
    public EigenschapRequestDtoValidator()
    {
        CascadeRuleFor(r => r.Naam).NotNull().NotEmpty().MaximumLength(20);
        CascadeRuleFor(r => r.Definitie).NotNull().NotEmpty().MaximumLength(255);
        CascadeRuleFor(r => r.Specificatie)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(s => s.Groep).MaximumLength(32);
                validator.CascadeRuleFor(s => s.Formaat).NotNull().IsEnumName(typeof(Formaat));
                validator.CascadeRuleFor(s => s.Lengte).NotNull().NotEmpty().MaximumLength(14);
                validator.CascadeRuleFor(s => s.Kardinaliteit).NotNull().NotEmpty().MaximumLength(3);

                validator.When(
                    s => s.Formaat == $"{Formaat.tekst}",
                    () => validator.CascadeRuleForEach(s => s.Waardenverzameling).MaximumLength(100)
                );
                validator.When(
                    s => s.Formaat == $"{Formaat.getal}",
                    () => validator.CascadeRuleForEach(s => s.Waardenverzameling).NotNull().NotEmpty().IsDecimal()
                );
                validator.When(
                    s => s.Formaat == $"{Formaat.datum}",
                    () => validator.CascadeRuleForEach(s => s.Waardenverzameling).IsDateWithoutSeparator()
                );
                validator.When(
                    s => s.Formaat == $"{Formaat.datum_tijd}",
                    () => validator.CascadeRuleForEach(s => s.Waardenverzameling).IsDateTimeWithoutSeparator()
                );
            });
        CascadeRuleFor(r => r.Toelichting).MaximumLength(1000);
        CascadeRuleFor(r => r.ZaakType).NotNull().IsUri();
        CascadeRuleFor(r => r.StatusType).IsUri();
        CascadeRuleFor(z => z.BeginGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.BeginObject).IsDate(false);
        CascadeRuleFor(z => z.EindeObject).IsDate(false);
    }
}
