using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.ServiceAgent.v1;

namespace Roxit.ZGW.Besluiten.Web.BusinessRules;

public class BesluitBusinessRuleService : IBesluitBusinessRuleService
{
    private readonly BrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IZakenServiceAgent _zakenServiceAgent;

    public BesluitBusinessRuleService(BrcDbContext context, ICatalogiServiceAgent catalogiServiceAgent, IZakenServiceAgent zakenServiceAgent)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _zakenServiceAgent = zakenServiceAgent;
    }

    public async Task<bool> ValidateAsync(
        Besluit besluitAdd,
        bool ignoreBesluitTypeValidation,
        bool ignoreZaakValidation,
        List<ValidationError> errors
    )
    {
        // 1. Garanderen uniciteit verantwoordelijke_organisatie en identificatie op de Besluit-resource (brc-002)
        if (!string.IsNullOrEmpty(besluitAdd.Identificatie))
        {
            if (
                await _context
                    .Besluiten.AsNoTracking()
                    .AnyAsync(b =>
                        b.Identificatie == besluitAdd.Identificatie && b.VerantwoordelijkeOrganisatie == besluitAdd.VerantwoordelijkeOrganisatie
                    )
            )
            {
                var error = new ValidationError(
                    "identificatie",
                    ErrorCode.IdentificationNotUnique,
                    "Deze identificatie bestaat al voor de verantwoordelijke organisatie."
                );

                errors.Add(error);
            }
        }

        // 2. Geldigheid besluittype URL - de resource moet opgevraagd kunnen worden uit de Catalogi API en de vorm van een BESLUITTYPE hebben (brc-001).
        if (!ignoreBesluitTypeValidation)
        {
            await ValidateBesluitTypeAsync(besluitAdd.BesluitType, errors);
        }

        // 3. Common rules (valid for Add Besluit and Update Besluit)
        return await ValidateAsync(besluitAdd, ignoreZaakValidation, errors);
    }

    public async Task<bool> ValidateAsync(Besluit besluitExisting, Besluit besluitUpdate, bool ignoreZaakValidation, List<ValidationError> errors)
    {
        if (besluitUpdate.Identificatie != null && besluitExisting.Identificatie != besluitUpdate.Identificatie)
        {
            var error = new ValidationError("identificatie", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);

            return false;
        }
        if (
            besluitUpdate.VerantwoordelijkeOrganisatie != null
            && besluitExisting.VerantwoordelijkeOrganisatie != besluitUpdate.VerantwoordelijkeOrganisatie
        )
        {
            var error = new ValidationError("verantwoordelijkeOrganisatie", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);

            return false;
        }

        // 1. Garanderen uniciteit verantwoordelijke_organisatie en identificatie op de Besluit-resource (brc-002)
        if (
            await _context
                .Besluiten.AsNoTracking()
                .AnyAsync(b =>
                    b.Id != besluitExisting.Id
                    && b.Identificatie == besluitUpdate.Identificatie
                    && b.VerantwoordelijkeOrganisatie == besluitUpdate.VerantwoordelijkeOrganisatie
                )
        )
        {
            var error = new ValidationError(
                "identificatie",
                ErrorCode.IdentificationNotUnique,
                "Deze identificatie bestaat al voor de verantwoordelijke organisatie."
            );

            errors.Add(error);
        }

        // Valideren besluittype op de Besluit - resource(brc - 001)
        if (besluitExisting.BesluitType != besluitUpdate.BesluitType)
        {
            var error = new ValidationError("besluittype", ErrorCode.UpdateNotAllowed, "Dit veld mag niet gewijzigd worden.");

            errors.Add(error);
        }

        // 2. Common rules (valid for Add Besluit and Update Besluit)
        return await ValidateAsync(besluitUpdate, ignoreZaakValidation, errors);
    }

    private async Task<bool> ValidateAsync(Besluit besluit, bool ignoreZaakValidation, List<ValidationError> errors)
    {
        // Common rules (both for add and update besluit)

        // 2. Geldigheid zaak URL - de resource moet opgevraagd kunnen worden uit de Zaken API en de vorm van een ZAAK hebben (optional)
        if (!ignoreZaakValidation && !string.IsNullOrEmpty(besluit.Zaak))
        {
            // Valideren informatieobject op de BesluitInformatieObject-resource (brc-003)
            await ValidateZaakAsync(besluit.Zaak, besluit.BesluitType, errors);
        }

        // 3. Datum in het verleden of nu
        if (besluit.Datum > DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            var error = new ValidationError("datum", ErrorCode.Invalid, "Datum moet vandaag of in het verleden zijn.");

            errors.Add(error);
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateBesluitTypeAsync(string besluitType, List<ValidationError> errors)
    {
        // Valideren besluittype op de Besluit - resource (brc-001)

        var result = await _catalogiServiceAgent.GetBesluitTypeByUrlAsync(besluitType);

        if (!result.Success)
        {
            errors.Add(new ValidationError("besluittype", result.Error.Code, result.Error.Title));
        }
        else
        {
            // Check BesluitType Concept flag
            if (result.Response.Concept)
            {
                string error = $"De URL {besluitType} heeft als status Concept.";

                errors.Add(new ValidationError("besluittype", ErrorCode.NotPublished, error));
            }
        }

        return errors.Count == 0;
    }

    private async Task<bool> ValidateZaakAsync(string zaak, string besluittype, List<ValidationError> errors)
    {
        var result = await _zakenServiceAgent.GetZaakByUrlAsync(zaak);

        if (!result.Success)
        {
            errors.Add(new ValidationError("zaak", result.Error.Code, result.Error.Title));
        }
        else
        {
            // Wanneer een Besluit bij een zaak hoort (Besluit.zaak is gezet), dan MOET Besluit.besluittype voorkomen in de Besluit.zaak.zaaktype.besluittypen. (brc-007)
            var zaaktype = await TryGetZaakTypeByUrlAsync(result.Response.Zaaktype, errors);
            if (zaaktype != null && !zaaktype.BesluitTypen.Any(t => t == besluittype))
            {
                string error = "De referentie hoort niet bij het zaaktype van de zaak.";

                errors.Add(new ValidationError("nonFieldErrors", ErrorCode.ZaakTypeMismatch, error));
            }
        }

        return errors.Count == 0;
    }

    private async Task<ZaakTypeResponseDto> TryGetZaakTypeByUrlAsync(string zaaktype, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaaktype);

        if (!result.Success)
        {
            errors.Add(new ValidationError("zaaktype", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }
}
