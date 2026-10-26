using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._7;

public class ZaakNotitieRequestValidator : ZGWValidator<ZaakNotitieRequestDto>
{
    public ZaakNotitieRequestValidator()
    {
        CascadeRuleFor(z => z.Onderwerp).NotNull().NotEmpty().MaximumLength(255);
        CascadeRuleFor(z => z.Tekst).NotNull().NotEmpty();
        CascadeRuleFor(z => z.AangemaaktDoor).MaximumLength(255);
        CascadeRuleFor(z => z.NotitieType).IsEnumName(typeof(ZaakNotitieType));
        CascadeRuleFor(z => z.Status).IsEnumName(typeof(ZaakNotitieStatus));
        CascadeRuleFor(z => z.GerelateerdAan).NotNull().NotEmpty().IsUri().MaximumLength(1000);
    }
}
