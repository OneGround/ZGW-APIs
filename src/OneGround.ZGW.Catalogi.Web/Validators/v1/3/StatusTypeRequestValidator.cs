using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3;

public class StatusTypeRequestValidator : ZGWValidator<StatusTypeRequestDto>
{
    public StatusTypeRequestValidator()
    {
        CascadeRuleFor(s => s.Omschrijving).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(s => s.OmschrijvingGeneriek).MaximumLength(80);
        CascadeRuleFor(s => s.StatusTekst).MaximumLength(80);
        CascadeRuleFor(s => s.ZaakType).NotNull().IsUri();
        CascadeRuleFor(s => s.Doorlooptijd).IsDuration();
        CascadeRuleFor(s => s.Toelichting).MaximumLength(1000);
        CascadeRuleForEach(z => z.CheckListItemStatustypes)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(v => v.ItemNaam).NotNull().NotEmpty().MaximumLength(30);
                validator.CascadeRuleFor(v => v.Toelichting).MaximumLength(1000);
                validator.CascadeRuleFor(v => v.Vraagstelling).NotNull().NotEmpty().MaximumLength(255);
            });
        CascadeRuleForEach(z => z.Eigenschappen).NotNull().IsUri();
        CascadeRuleFor(s => s.BeginGeldigheid).IsDate(false);
        CascadeRuleFor(s => s.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(s => s.BeginObject).IsDate(false);
        CascadeRuleFor(s => s.EindeObject).IsDate(false);
    }
}
