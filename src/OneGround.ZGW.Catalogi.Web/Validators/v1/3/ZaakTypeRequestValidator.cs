using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1._3;

public class ZaakTypeRequestValidator : ZGWValidator<ZaakTypeRequestDto>
{
    public ZaakTypeRequestValidator()
    {
        CascadeRuleFor(z => z.Identificatie).NotNull().NotEmpty().IsValidIdentificatie();
        CascadeRuleFor(z => z.Omschrijving).NotNull().NotEmpty().MaximumLength(80);
        CascadeRuleFor(z => z.OmschrijvingGeneriek).MaximumLength(80);
        CascadeRuleFor(z => z.VertrouwelijkheidAanduiding).NotNull().IsEnumName(typeof(VertrouwelijkheidAanduiding));
        CascadeRuleFor(z => z.Doel).NotNull().NotEmpty();
        CascadeRuleFor(z => z.Aanleiding).NotNull().NotEmpty();
        CascadeRuleFor(z => z.IndicatieInternOfExtern).NotNull().IsEnumName(typeof(IndicatieInternOfExtern));
        CascadeRuleFor(z => z.HandelingInitiator).NotNull().MaximumLength(20);
        CascadeRuleFor(z => z.Onderwerp).NotNull().MaximumLength(80);
        CascadeRuleFor(z => z.HandelingBehandelaar).NotNull().MaximumLength(20);
        CascadeRuleFor(z => z.Doorlooptijd).NotNull().IsDuration();
        CascadeRuleFor(z => z.Servicenorm).IsDuration();
        CascadeRuleFor(z => z.OpschortingEnAanhoudingMogelijk).NotNull();
        CascadeRuleFor(z => z.VerlengingMogelijk).NotNull();
        CascadeRuleFor(z => z.VerlengingsTermijn).IsDuration();
        CascadeRuleFor(z => z.PublicatieIndicatie).NotNull();
        CascadeRuleFor(z => z.ProductenOfDiensten).NotNull().IsDistinct();
        CascadeRuleForEach(z => z.ProductenOfDiensten).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.ReferentieProces)
            .NotNull()
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(z => z.Naam).NotNull().NotEmpty().MaximumLength(80);
                validator.CascadeRuleFor(z => z.Link).MaximumLength(200).IsUri();
            });
        CascadeRuleFor(z => z.SelectielijstProcestype).MaximumLength(200).IsUri();
        CascadeRuleFor(z => z.Catalogus).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.BesluitTypen).NotNull().IsDistinct();
        CascadeRuleForEach(z => z.BesluitTypen).NotNull().IsNotUri();
        CascadeRuleFor(z => z.DeelZaakTypen).NotNull().IsDistinct();
        CascadeRuleForEach(z => z.DeelZaakTypen).NotNull().IsNotUri();
        CascadeRuleFor(z => z.GerelateerdeZaakTypen).NotNull();
        CascadeRuleForEach(z => z.GerelateerdeZaakTypen)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(v => v.ZaakType).NotNull().NotEmpty().MaximumLength(200).IsNotUri();
                validator.CascadeRuleFor(v => v.AardRelatie).NotNull().NotEmpty().IsEnumName(typeof(AardRelatie));
                validator.CascadeRuleFor(v => v.Toelichting).MaximumLength(255);
            });
        CascadeRuleFor(z => z.BeginGeldigheid).IsDate(true);
        CascadeRuleFor(z => z.EindeGeldigheid).IsDate(false);
        CascadeRuleFor(z => z.VersieDatum).IsDate(true);
        CascadeRuleFor(z => z.Verantwoordelijke).MaximumLength(50);
        CascadeRuleFor(z => z.BeginObject).IsDate(false);
        CascadeRuleFor(z => z.EindeObject).IsDate(false);
        CascadeRuleFor(z => z.BronCatalogus)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(z => z.Url).NotNull().NotEmpty().MaximumLength(200).IsUri();
                validator.CascadeRuleFor(z => z.Domein).NotNull().NotEmpty().MaximumLength(5);
                validator.CascadeRuleFor(z => z.Rsin).IsRsin(required: false);
            });
        CascadeRuleFor(z => z.BronZaaktype)
            .ChildRules(validator =>
            {
                validator.CascadeRuleFor(z => z.Url).NotNull().NotEmpty().MaximumLength(200).IsUri();
                validator.CascadeRuleFor(z => z.Identificatie).NotNull().NotEmpty().MaximumLength(50);
                validator.CascadeRuleFor(z => z.Omschrijving).NotNull().NotEmpty().MaximumLength(80);
            });
    }
}
