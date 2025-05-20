using FluentValidation;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Common.Web.Validations;

namespace OneGround.ZGW.Catalogi.Web.Validators.v1;

public class RolTypeRequestDtoValidator : ZGWValidator<RolTypeRequestDto>
{
    public RolTypeRequestDtoValidator()
    {
        CascadeRuleFor(r => r.ZaakType).NotNull().IsUri();
        CascadeRuleFor(r => r.Omschrijving).NotNull().NotEmpty().MaximumLength(100);
        CascadeRuleFor(r => r.OmschrijvingGeneriek).NotNull().NotEmpty().IsEnumName(typeof(Common.DataModel.OmschrijvingGeneriek));
    }
}
