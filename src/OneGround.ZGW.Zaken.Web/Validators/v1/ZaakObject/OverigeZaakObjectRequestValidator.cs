using FluentValidation;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public class OverigeZaakObjectRequestValidator : ZGWValidator<OverigeZaakObjectRequestDto>
{
    public OverigeZaakObjectRequestValidator()
    {
        Include(new ZaakObjectRequestValidator());
        CascadeRuleFor(z => z.ObjectIdentificatie).NotNull().NotEmpty().SetValidator(new OverigeZaakObjectDtoValidator());
    }
}

public class OverigeZaakObjectDtoValidator : ZGWValidator<OverigeZaakObjectDto>
{
    public OverigeZaakObjectDtoValidator()
    {
        CascadeRuleFor(o => o.OverigeData)
            .NotNull()
            .Must(token =>
            {
                // Reject blank string values
                if (token is JValue jv && jv.Type == JTokenType.String && string.IsNullOrWhiteSpace((string)jv))
                {
                    return false;
                }

                // Reject empty objects: {}
                if (token is JObject obj && !obj.HasValues)
                {
                    return false;
                }

                // Reject empty arrays: []
                if (token is JArray arr && !arr.HasValues)
                {
                    return false;
                }

                return true;
            })
            .WithErrorCode(ErrorCode.Blank);
    }
}
