using FluentValidation;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class KadastraleOnroerendeZaakObjectRequestValidator : ZGWValidator<KadastraleOnroerendeZaakObjectRequestDto>
{
    public KadastraleOnroerendeZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new KadastraleOnroerendeZaakObjectDtoValidator());
    }
}

public class KadastraleOnroerendeZaakObjectDtoValidator : ZGWValidator<KadastraleOnroerendeZaakObjectDto>
{
    public KadastraleOnroerendeZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.KadastraleAanduiding).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(o => o.KadastraleIdentificatie).NotNull().NotEmpty().MaximumLength(1000);
    }
}
