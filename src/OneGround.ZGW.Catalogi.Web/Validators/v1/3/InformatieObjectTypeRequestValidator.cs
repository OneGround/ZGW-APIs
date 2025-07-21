using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3;

public class InformatieObjectTypeRequestValidator : ZGWValidator<InformatieObjectTypeRequestDto>
{
    public InformatieObjectTypeRequestValidator()
    {
        CascadeRuleFor(r => r.Catalogus).NotNull().IsUri();
        CascadeRuleFor(r => r.Omschrijving).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(r => r.VertrouwelijkheidAanduiding).NotNull().IsEnumName(typeof(VertrouwelijkheidAanduiding));
        CascadeRuleFor(r => r.BeginGeldigheid).IsDate(true);
        CascadeRuleFor(r => r.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(r => r.BeginObject).IsDate(false);
        CascadeRuleFor(r => r.EindeObject).IsDate(false);
        CascadeRuleFor(r => r.InformatieObjectCategorie).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(r => r.Trefwoord).ForEach(r => r.MaximumLength(30));
        CascadeRuleFor(z => z.OmschrijvingGeneriek)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(z => z.InformatieObjectTypeOmschrijvingGeneriek).NotNull().NotEmpty().MaximumLength(80);
                validator.CascadeRuleFor(z => z.DefinitieInformatieObjectTypeOmschrijvingGeneriek).NotNull().NotEmpty().MaximumLength(255);
                validator.CascadeRuleFor(z => z.HerkomstInformatieObjectTypeOmschrijvingGeneriek).NotNull().NotEmpty().MaximumLength(12);
                validator.CascadeRuleFor(z => z.HierarchieInformatieObjectTypeOmschrijvingGeneriek).NotNull().NotEmpty().MaximumLength(80);
                validator.CascadeRuleFor(z => z.OpmerkingInformatieObjectTypeOmschrijvingGeneriek).MaximumLength(255);
            });
    }
}
