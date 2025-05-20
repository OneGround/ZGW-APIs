using FluentValidation.Results;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;

namespace OneGround.ZGW.Zaken.Web.Validators.v1.ZaakObject;

public interface IZaakObjectValidatorService
{
    bool Validate(
        ZaakObjectRequestDto zaakobjectRequest,
        out ValidationResult validationResult,
        DataModel.ZaakObject.ZaakObject currentZaakobject = null
    );

    bool IsValidAdresZaakObject(AdresZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidBuurtZaakObject(BuurtZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidGemeenteZaakObject(GemeenteZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidKadastraleOnroerendeZaakObject(KadastraleOnroerendeZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidOverigeZaakObject(OverigeZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidPandZaakObject(PandZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidTerreinGebouwdObjectZaakObject(TerreinGebouwdObjectZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidWozWaardeZaakObject(WozWaardeZaakObjectDto request, out ValidationResult validationResult);
}
