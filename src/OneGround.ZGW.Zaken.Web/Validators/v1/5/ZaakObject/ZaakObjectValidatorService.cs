using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Validations;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1._5.ZaakObject;

public class ZaakObjectValidatorService : IZaakObjectValidatorService
{
    private readonly IValidatorService _validatorService;

    public ZaakObjectValidatorService(IValidatorService validatorService)
    {
        _validatorService = validatorService;
    }

    public bool Validate(
        ZaakObjectRequestDto zaakobjectRequest,
        out ValidationResult validationResult,
        DataModel.ZaakObject.ZaakObject currentZaakobject = null
    )
    {
        _validatorService.IsValid(zaakobjectRequest, out validationResult);

        // Note: For v1.2 we have a new ObjectTypeOverigeDefinitie to be validated against the same datacontract ZaakObjectRequestDto
        validationResult.Errors.AddRange(IsValidObjectTypeOverigeDefinitie(zaakobjectRequest).Errors);

        if (currentZaakobject != null)
        {
            const string message = "Veld mag niet gewijzigd worden.";
            if (!zaakobjectRequest.Zaak.EndsWith(currentZaakobject.Zaak.Url))
            {
                validationResult.Errors.Add(new ValidationFailure("zaak", message) { ErrorCode = ErrorCode.UpdateNotAllowed });
            }
            if (currentZaakobject.Object != zaakobjectRequest.Object)
            {
                validationResult.Errors.Add(new ValidationFailure("object", message) { ErrorCode = ErrorCode.UpdateNotAllowed });
            }
            if (currentZaakobject.ObjectType.ToString() != zaakobjectRequest.ObjectType)
            {
                validationResult.Errors.Add(new ValidationFailure("objecttype", message) { ErrorCode = ErrorCode.UpdateNotAllowed });
            }
        }

        return validationResult.IsValid;
    }

    private static ValidationResult IsValidObjectTypeOverigeDefinitie(ZaakObjectRequestDto request)
    {
        var validator = new InlineValidator<ZaakObjectRequestDto>
        {
            v =>
                v.CascadeRuleFor(o => o.ObjectTypeOverigeDefinitie)
                    .ChildRules(v =>
                    {
                        v.CascadeRuleFor(o => o.Url).NotNull().NotEmpty().IsUri().MaximumLength(1000);
                        v.CascadeRuleFor(o => o.Schema).NotNull().NotEmpty().MaximumLength(100);
                        v.CascadeRuleFor(o => o.ObjectData).NotNull().NotEmpty().MaximumLength(100);
                    })
                    .Unless(v => v.ObjectTypeOverigeDefinitie == null),
        };
        return validator.Validate(request);
    }

    public bool IsValidAdresZaakObject(Zaken.Contracts.v1.AdresZaakObjectDto request, out ValidationResult validationResult)
    {
        validationResult = new ValidationResult();

        var validator = new AdresZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidBuurtZaakObject(Zaken.Contracts.v1.BuurtZaakObjectDto request, out ValidationResult validationResult)
    {
        validationResult = new ValidationResult();

        var validator = new BuurtZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidGemeenteZaakObject(Zaken.Contracts.v1.GemeenteZaakObjectDto request, out ValidationResult validationResult)
    {
        validationResult = new ValidationResult();

        var validator = new GemeenteZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidKadastraleOnroerendeZaakObject(
        Zaken.Contracts.v1.KadastraleOnroerendeZaakObjectDto request,
        out ValidationResult validationResult
    )
    {
        validationResult = new ValidationResult();

        var validator = new KadastraleOnroerendeZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidOverigeZaakObject(Zaken.Contracts.v1.OverigeZaakObjectDto request, out ValidationResult validationResult)
    {
        validationResult = new ValidationResult();

        var validator = new OverigeZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidPandZaakObject(Zaken.Contracts.v1.PandZaakObjectDto request, out ValidationResult validationResult)
    {
        validationResult = new ValidationResult();

        var validator = new PandZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidTerreinGebouwdObjectZaakObject(
        Zaken.Contracts.v1.TerreinGebouwdObjectZaakObjectDto request,
        out ValidationResult validationResult
    )
    {
        validationResult = new ValidationResult();

        var validator = new TerreinGebouwdObjectZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }

    public bool IsValidWozWaardeZaakObject(Zaken.Contracts.v1.WozWaardeZaakObjectDto request, out ValidationResult validationResult)
    {
        validationResult = new ValidationResult();

        var validator = new WozWaardeZaakObjectDtoValidator();

        var result = validator.Validate(request);

        validationResult.Errors.AddRange(result.Errors);

        return validationResult.Errors.Count == 0;
    }
}
