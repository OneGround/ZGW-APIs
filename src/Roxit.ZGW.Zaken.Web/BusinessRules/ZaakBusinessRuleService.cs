using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Extensions;
using Roxit.ZGW.Common.ServiceAgent;
using Roxit.ZGW.Common.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.ServiceAgent.v1;
using Roxit.ZGW.Notificaties.ServiceAgent;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.ServiceAgent.v1;

namespace Roxit.ZGW.Zaken.Web.BusinessRules;

public class ZaakBusinessRuleService : IZaakBusinessRuleService
{
    private readonly ZrcDbContext _context;
    private readonly IEntityUriService _uriService;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly IDocumentenServiceAgent _documentenServiceAgent;
    private readonly INotificatiesServiceAgent _notificatiesServiceAgent;

    public ZaakBusinessRuleService(
        ZrcDbContext context,
        IEntityUriService uriService,
        ICatalogiServiceAgent catalogiServiceAgent,
        IZakenServiceAgent zakenServiceAgent,
        IDocumentenServiceAgent documentenServiceAgent,
        INotificatiesServiceAgent notificatiesServiceAgent
    )
    {
        _context = context;
        _uriService = uriService;
        _catalogiServiceAgent = catalogiServiceAgent;
        _zakenServiceAgent = zakenServiceAgent;
        _documentenServiceAgent = documentenServiceAgent;
        _notificatiesServiceAgent = notificatiesServiceAgent;
    }

    public async Task<bool> ValidateAsync(Zaak zaakAdd, string hoofdzaakUrl, bool ignoreZaakTypeValidation, List<ValidationError> errors)
    {
        // 1. Add rules
        if (!string.IsNullOrEmpty(zaakAdd.Identificatie))
        {
            if (
                await _context
                    .Zaken.AsNoTracking()
                    .AnyAsync(z => z.Identificatie == zaakAdd.Identificatie && z.Bronorganisatie == zaakAdd.Bronorganisatie)
            )
            {
                var error = new ValidationError(
                    "identificatie",
                    ErrorCode.IdentificationNotUnique,
                    "Deze identificatie bestaat al voor deze bronorganisatie."
                );

                errors.Add(error);
            }
        }

        // zrc-014: LaatsteBetaaldatum mag niet gezet worden als de betalingsindicatie "nvt" is.
        if (zaakAdd.LaatsteBetaaldatum.HasValue && zaakAdd.BetalingsIndicatie == BetalingsIndicatie.nvt)
        {
            var error = new ValidationError(
                "laatsteBetaaldatum",
                ErrorCode.BetalingNvt,
                $"Datum mag niet gezet worden omdat de betalingsindicatie '{BetalingsIndicatie.nvt}' is."
            );

            errors.Add(error);
        }

        ServiceAgentResponse<ZaakTypeResponseDto> validatedZaatType = null;
        if (!ignoreZaakTypeValidation)
        {
            validatedZaatType = await ValidateZaakType(zaakAdd.Zaaktype, errors);
        }

        // 2. Common rules (valid for Add Zaak and Update Zaak)
        return await ValidateZaakAsync(null, zaakAdd, hoofdzaakUrl, errors, validatedZaatType);
    }

    public async Task<bool> ValidateAsync(Zaak zaakExisting, Zaak zaakUpdate, string hoofdzaakUrl, List<ValidationError> errors)
    {
        // 1. Update rules
        if (!string.IsNullOrEmpty(zaakUpdate.Identificatie) && zaakExisting.Identificatie != zaakUpdate.Identificatie)
        {
            var error = new ValidationError("identificatie", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);
        }

        if (zaakExisting.Zaaktype != zaakUpdate.Zaaktype)
        {
            var error = new ValidationError("zaaktype", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);
        }

        // zrc-013: Valideren hoofdzaak op de Zaak-resource
        if (hoofdzaakUrl != null && hoofdzaakUrl == _uriService.GetUri(zaakExisting))
        {
            var error = new ValidationError("hoofdzaak", ErrorCode.SelfForbidden, "Parent case can not be the same as child case.");
            errors.Add(error);
        }

        // 2. Common rules (valid for Add Zaak and Update Zaak)
        return await ValidateZaakAsync(zaakExisting, zaakUpdate, hoofdzaakUrl, errors);
    }

    public async Task<bool> ValidateZaakDocumentenArchivedStatusAsync(Zaak existingZaak, List<ValidationError> errors)
    {
        // zrc-022: Archiefstatus kan alleen een waarde anders dan "nog_te_archiveren" hebben indien
        // van alle gerelateeerde INFORMATIEOBJECTen het attribuut status de waarde "gearchiveerd" heeft.
        foreach (var url in existingZaak.ZaakInformatieObjecten.Select(z => z.InformatieObject))
        {
            var result = await _documentenServiceAgent.GetEnkelvoudigInformatieObjectByUrlAsync(url);
            if (!result.Success)
            {
                errors.Add(new ValidationError("informatieobject", result.Error.Code, result.Error.Title));
            }
            else if (!ResourceTypeValidator.IsOfType("enkelvoudiginformatieobjecten", url))
            {
                errors.Add(
                    new ValidationError(
                        "informatieobject",
                        ErrorCode.InvalidResource,
                        $"De URL {url} resource lijkt niet op de gespecificeerde entiteit. Geef een validate URL op."
                    )
                );
            }
            else
            {
                // Check if information object is archived
                if (result.Response.Status != ArchiefStatus.gearchiveerd.ToString())
                {
                    var error = new ValidationError(
                        "nonFieldErrors",
                        ErrorCode.DocumentsNotArchived,
                        $"Er zijn gerelateerde informatieobjecten waarvan de 'status' nog niet gelijk is aan '{ArchiefStatus.gearchiveerd}'. "
                            + $"Dit is een voorwaarde voor het zetten van de 'archiefstatus' op een andere waarde dan '{ArchiefStatus.nog_te_archiveren}'."
                    );

                    errors.Add(error);

                    return false;
                }
            }
        }
        return true;
    }

    private async Task<bool> ValidateZaakAsync(
        Zaak zaakExisting,
        Zaak zaakForValidation,
        string hoofdzaakUrl,
        List<ValidationError> errors,
        ServiceAgentResponse<ZaakTypeResponseDto> validateZaaktype = null
    )
    {
        // zrc-013: Valideren hoofdzaak op de Zaak-resource
        if (!string.IsNullOrEmpty(hoofdzaakUrl))
        {
            var hoofdZaak = await _context.Zaken.AsNoTracking().FirstOrDefaultAsync(z => z.Id == _uriService.GetId(hoofdzaakUrl));

            if (hoofdZaak == null)
            {
                var error = new ValidationError("hoofdzaak", ErrorCode.NoMatch, "Dit veld bevat een niet bestaande Zaak.");
                errors.Add(error);
            }
            else if (hoofdZaak.HoofdzaakId.HasValue || (zaakExisting != null && zaakExisting.Deelzaken.Count != 0))
            {
                var error = new ValidationError("hoofdzaak", ErrorCode.DeelZaakSame, "Deelzaken van deelzaken zijn NIET toegestaan.");
                errors.Add(error);
            }
        }

        // zrc-011: Valideren relevanteAndereZaken op de Zaak-resource
        foreach (var (url, index) in zaakForValidation.RelevanteAndereZaken.Select(z => z.Url).WithIndex())
        {
            await ValidateRelevanteAndereZaak(url, index, errors);
        }

        // LaatsteBetaaldatum mag niet in de toekomst liggen.
        if (zaakForValidation.LaatsteBetaaldatum.HasValue && zaakForValidation.LaatsteBetaaldatum > DateTime.UtcNow)
        {
            var error = new ValidationError("laatsteBetaaldatum", ErrorCode.BetalingNvt, "Datum mag niet in de toekomst liggen.");

            errors.Add(error);
        }

        await ValidateProductenOfDiensten(zaakForValidation.Zaaktype, zaakForValidation, errors, validateZaaktype);

        if (!string.IsNullOrEmpty(zaakForValidation.Communicatiekanaal))
        {
            await ValidateCommunicatieKanaal(zaakForValidation.Communicatiekanaal, errors);
        }

        // zrc-022: verify that all existing information objects for current zaak are archived
        await ValidateZaakArchivedStatusAsync(zaakExisting?.Id, zaakForValidation, errors);

        return errors.Count == 0;
    }

    private async Task<bool> ValidateZaakArchivedStatusAsync(Guid? zaakId, Zaak zaak, List<ValidationError> errors)
    {
        // validate only existing zaak for related information objects
        if (zaakId.HasValue && zaak.Archiefstatus != ArchiefStatus.nog_te_archiveren)
        {
            var existingZaak = await _context.Zaken.AsNoTracking().Include(z => z.ZaakInformatieObjecten).FirstAsync(z => z.Id == zaakId);

            // zrc-022: Archiefstatus kan alleen een waarde anders dan "nog_te_archiveren" hebben indien
            // van alle gerelateeerde INFORMATIEOBJECTen het attribuut status de waarde "gearchiveerd" heeft.
            await ValidateZaakDocumentenArchivedStatusAsync(existingZaak, errors);
        }

        // zrc-022: Archiefnominatie moet een waarde hebben indien archiefstatus niet de waarde "nog_te_archiveren" heeft
        if (!zaak.Archiefnominatie.HasValue && zaak.Archiefstatus != ArchiefStatus.nog_te_archiveren)
        {
            var error = new ValidationError(
                "archiefnominatie",
                ErrorCode.ArchiefNominatieNotSet,
                $"Moet van een waarde voorzien zijn als de 'Archiefstatus' een waarde heeft anders dan '{ArchiefStatus.nog_te_archiveren}'."
            );

            errors.Add(error);
        }

        // zrc-022: Archiefactiedatum moet een waarde hebben indien archiefstatus niet de waarde "nog_te_archiveren" heeft óf
        //  new:    Archiefactiedatum mag leeg zijn als de archiefstatus de waarde "gearchiveerd_procestermijn_onbekend" heeft
        if (
            !zaak.Archiefactiedatum.HasValue && (zaak.Archiefstatus == ArchiefStatus.gearchiveerd || zaak.Archiefstatus == ArchiefStatus.overgedragen)
        )
        {
            var error = new ValidationError(
                "archiefactiedatum",
                ErrorCode.ArchiefActieDatumNotSet,
                $"Moet van een waarde voorzien zijn als de 'Archiefstatus' een waarde heeft anders dan '{ArchiefStatus.nog_te_archiveren}'. Of mag leeg zijn als de 'Archiefstatus' de waarde '{ArchiefStatus.gearchiveerd_procestermijn_onbekend}' heeft."
            );

            errors.Add(error);
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateCommunicatieKanaal(string communicatieKanaalUrl, List<ValidationError> errors)
    {
        var result = await _notificatiesServiceAgent.GetKanaalByUrl(communicatieKanaalUrl);

        if (!result.Success)
        {
            errors.Add(new ValidationError("communicatiekanaal", result.Error.Code, result.Error.Title));
        }
        else if (!ResourceTypeValidator.IsOfType("communicatiekanalen", communicatieKanaalUrl))
        {
            errors.Add(
                new ValidationError(
                    "communicatiekanaal",
                    ErrorCode.InvalidResource,
                    $"De URL {communicatieKanaalUrl} resource lijkt niet op de gespecificeerde entiteit. Geef een validate URL op."
                )
            );
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateProductenOfDiensten(
        string zaaktype,
        Zaak zaak,
        List<ValidationError> errors,
        ServiceAgentResponse<ZaakTypeResponseDto> validateZaaktype = null
    )
    {
        if (zaak.ProductenOfDiensten.Count != 0)
        {
            if (validateZaaktype == null)
            {
                var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaaktype);
                if (!result.Success)
                {
                    errors.Add(new ValidationError("zaaktype", result.Error.Code, result.Error.Title));
                }
                else if (!ResourceTypeValidator.IsOfType("zaaktypen", zaaktype))
                {
                    errors.Add(
                        new ValidationError(
                            "zaaktype",
                            ErrorCode.InvalidResource,
                            $"De URL {zaaktype} resource lijkt niet op de gespecificeerde entiteit. Geef een validate URL op."
                        )
                    );
                }
                else
                {
                    validateZaaktype = result;
                }
            }
            if (validateZaaktype != null && validateZaaktype.Success)
            {
                var zaakProductenOfDienstenIsValid = zaak.ProductenOfDiensten.All(p => validateZaaktype.Response.ProductenOfDiensten.Contains(p));

                if (!zaakProductenOfDienstenIsValid)
                {
                    string error = "Niet alle producten/diensten komen voor in de producten/diensten op het zaaktype";

                    errors.Add(new ValidationError("productenOfDiensten", ErrorCode.InvalidProductsServices, error));
                }
            }
        }

        return errors.Count == 0;
    }

    private async Task<ServiceAgentResponse<ZaakTypeResponseDto>> ValidateZaakType(string zaaktype, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaaktype);
        if (!result.Success)
        {
            errors.Add(new ValidationError("zaaktype", result.Error.Code, result.Error.Title));
        }
        else if (!ResourceTypeValidator.IsOfType("zaaktypen", zaaktype))
        {
            errors.Add(
                new ValidationError(
                    "zaaktype",
                    ErrorCode.InvalidResource,
                    $"De URL {zaaktype} resource lijkt niet op de gespecificeerde entiteit. Geef een validate URL op."
                )
            );
        }
        else
        {
            // Check zaaktype Concept flag
            if (result.Response.Concept)
            {
                string error = $"De URL {zaaktype} heeft als status Concept.";

                errors.Add(new ValidationError("zaaktype", ErrorCode.NotPublished, error));
            }
        }

        return result;
    }

    private async Task ValidateRelevanteAndereZaak(string zaakUrl, int index, List<ValidationError> errors)
    {
        var result = await _zakenServiceAgent.GetZaakByUrlAsync(zaakUrl);

        if (!result.Success)
        {
            errors.Add(new ValidationError($"relevanteAndereZaken.{index}.url", result.Error.Code, result.Error.Title));
        }
        else if (!ResourceTypeValidator.IsOfType("zaken", zaakUrl))
        {
            errors.Add(
                new ValidationError(
                    $"relevanteAndereZaken.{index}.url",
                    ErrorCode.InvalidResource,
                    $"De URL {zaakUrl} resource lijkt niet op de gespecificeerde entiteit. Geef een validate URL op."
                )
            );
        }
    }
}
