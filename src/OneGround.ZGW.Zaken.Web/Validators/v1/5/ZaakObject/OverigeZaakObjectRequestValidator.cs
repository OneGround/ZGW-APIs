using FluentValidation;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakObject;

public class OverigeZaakObjectRequestValidator : ZGWValidator<OverigeZaakObjectRequestDto>
{
    public OverigeZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new OverigeZaakObjectDtoValidator());
    }
}

public class OverigeZaakObjectDtoValidator : ZGWValidator<Zaken.Contracts.v1.OverigeZaakObjectDto>
{
    public OverigeZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.OverigeData)
            .NotNull()
            .Must(token =>
            {
                // Reject blank strings
                if (token is JValue jv && jv.Type == JTokenType.String)
                {
                    return !string.IsNullOrWhiteSpace((string)jv);
                }

                // Reject empty objects and arrays
                if ((token.Type == JTokenType.Object || token.Type == JTokenType.Array) && !token.HasValues)
                {
                    return false;
                }

                // Accept all other non-null tokens
                return true;
            })
            .WithErrorCode(ErrorCode.Blank);
    }
}
