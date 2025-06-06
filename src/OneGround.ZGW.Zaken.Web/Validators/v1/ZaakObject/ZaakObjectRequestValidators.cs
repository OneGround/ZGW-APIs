using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class ZaakObjectRequestValidator : ZGWValidator<ZaakObjectRequestDto>
{
    public ZaakObjectRequestValidator()
    {
        CascadeRuleFor(z => z.Zaak).NotNull().NotEmpty().IsUri();
        CascadeRuleFor(z => z.Object).IsUri().MaximumLength(1000);
        CascadeRuleFor(z => z.ObjectType).NotNull().NotEmpty().IsEnumName(typeof(ObjectType));

        When(
                z => z.ObjectType == ObjectType.overige.ToString(),
                () =>
                {
                    CascadeRuleFor(z => z.ObjectTypeOverige).NotNull().MaximumLength(100).Matches(@"[a-z_]+");
                }
            )
            .Otherwise(() =>
            {
                CascadeRuleFor(z => z.ObjectTypeOverige).Null();
            });

        CascadeRuleFor(z => z.RelatieOmschrijving).MaximumLength(80);
    }
}
