using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.ServiceAgent.v1;

namespace Roxit.ZGW.Zaken.Web.Services.BronDate;

public class GerelateerdeZaakBronDate : IBronDateService
{
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly ZrcDbContext _context;

    public GerelateerdeZaakBronDate(IZakenServiceAgent zakenServiceAgent, ZrcDbContext context)
    {
        _zakenServiceAgent = zakenServiceAgent;
        _context = context;
    }

    public async Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> errors, CancellationToken cancellationToken)
    {
        var relevanteAndereZaken = await _context.RelevanteAndereZaken.AsNoTracking().Where(z => z.ZaakId == zaak.Id).ToListAsync(cancellationToken);

        if (relevanteAndereZaken.Count == 0)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    "Geen gerelateerde zaken aan zaak gekoppeld om brondatum uit af te leiden."
                )
            );
            return null;
        }

        var maxDate = DateOnly.MinValue;
        foreach (var relevantZaakUrl in relevanteAndereZaken.Select(z => z.Url))
        {
            var relevantZaak = await GetZaakAsync(relevantZaakUrl, errors);
            if (string.IsNullOrEmpty(relevantZaak.Einddatum))
                continue;

            var date = DateOnly.Parse(relevantZaak.Einddatum);
            if (date > maxDate)
            {
                maxDate = date;
            }
        }

        if (maxDate == DateOnly.MinValue)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    "Zaak.einddatum moet voor minstens een relevante zaak gezet worden voordat de zaak kan worden afgesloten."
                )
            );
            return null;
        }

        return maxDate;
    }

    private async Task<ZaakResponseDto> GetZaakAsync(string zaakUrl, List<ArchiveValidationError> errors)
    {
        var result = await _zakenServiceAgent.GetZaakByUrlAsync(zaakUrl);

        if (!result.Success)
        {
            errors.Add(new ArchiveValidationError("zaak", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }
}
