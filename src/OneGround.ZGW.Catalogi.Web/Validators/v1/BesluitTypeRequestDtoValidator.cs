using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1;

public class BesluitTypeRequestDtoValidator : ZGWValidator<BesluitTypeRequestDto>
{
    public BesluitTypeRequestDtoValidator()
    {
        CascadeRuleFor(r => r.Catalogus).NotNull().NotEmpty().IsUri();
        // Note: See proposal which is send to VNG to not support bi-directional relations between BT->ZT (ZT->BT does). So we comment out (temporary)
        //CascadeRuleFor(z => z.ZaakTypen).NotNull().IsDistinct(); // Note: Gives a validation error when not specified (or if is null is specified)
        //CascadeRuleForEach(z => z.ZaakTypen).NotNull().NotEmpty().IsUri();
        // ----
        CascadeRuleFor(r => r.Omschrijving).MaximumLength(80);
        CascadeRuleFor(r => r.OmschrijvingGeneriek).MaximumLength(80);
        CascadeRuleFor(r => r.BesluitCategorie).MaximumLength(40);
        CascadeRuleFor(r => r.ReactieTermijn).IsDuration();
        CascadeRuleFor(r => r.PublicatieTermijn).IsDuration();
        CascadeRuleFor(z => z.InformatieObjectTypen).NotNull().IsDistinct(); // Note: Gives a validation error when not specified (or if is null is specified)
        CascadeRuleForEach(z => z.InformatieObjectTypen).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(r => r.BeginGeldigheid).IsDate(true);
        CascadeRuleFor(r => r.EindeGeldigheid).IsDate(false);
    }
}
