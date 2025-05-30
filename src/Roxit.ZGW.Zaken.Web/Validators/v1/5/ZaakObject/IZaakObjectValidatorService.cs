using FluentValidation.Results;
using Roxit.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;

namespace Roxit.ZGW.Zaken.Web.Validators.v1._5.ZaakObject;

public interface IZaakObjectValidatorService
{
    bool Validate(
        ZaakObjectRequestDto zaakobjectRequest,
        out ValidationResult validationResult,
        DataModel.ZaakObject.ZaakObject currentZaakobject = null
    );

    bool IsValidAdresZaakObject(Zaken.Contracts.v1.AdresZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidBuurtZaakObject(Zaken.Contracts.v1.BuurtZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidGemeenteZaakObject(Zaken.Contracts.v1.GemeenteZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidKadastraleOnroerendeZaakObject(Zaken.Contracts.v1.KadastraleOnroerendeZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidOverigeZaakObject(Zaken.Contracts.v1.OverigeZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidPandZaakObject(Zaken.Contracts.v1.PandZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidTerreinGebouwdObjectZaakObject(Zaken.Contracts.v1.TerreinGebouwdObjectZaakObjectDto request, out ValidationResult validationResult);
    bool IsValidWozWaardeZaakObject(Zaken.Contracts.v1.WozWaardeZaakObjectDto request, out ValidationResult validationResult);
}
