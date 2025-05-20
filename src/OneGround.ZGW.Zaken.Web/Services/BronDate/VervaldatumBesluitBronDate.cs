using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Besluiten.ServiceAgent.v1;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Services.BronDate;

public class VervaldatumBesluitBronDate : BesluitBronDate, IBronDateService
{
    private readonly ZrcDbContext _context;

    public VervaldatumBesluitBronDate(IBesluitenServiceAgent besluitenServiceAgent, ZrcDbContext context)
        : base(besluitenServiceAgent)
    {
        _context = context;
    }

    public async Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> errors, CancellationToken cancellationToken)
    {
        var zaakBesluiten = await _context.ZaakBesluiten.AsNoTracking().Where(z => z.ZaakId == zaak.Id).ToListAsync(cancellationToken);

        if (zaakBesluiten.Count == 0)
        {
            errors.Add(
                new ArchiveValidationError(
                    string.Empty,
                    ErrorCode.ArchiefActieDatumError,
                    "Geen besluiten aan zaak gekoppeld om brondatum uit af te leiden."
                )
            );
            return null;
        }

        var maxDate = DateOnly.MinValue;
        foreach (var zaakBesluitUrl in zaakBesluiten.Select(b => b.Besluit))
        {
            var besluit = await GetBesluitAsync(zaakBesluitUrl, errors);
            if (string.IsNullOrEmpty(besluit.VervalDatum))
                continue;

            var date = DateOnly.Parse(besluit.VervalDatum);
            if (date > maxDate)
            {
                maxDate = date;
            }
        }

        if (maxDate != DateOnly.MinValue)
            return maxDate;

        errors.Add(
            new ArchiveValidationError(
                string.Empty,
                ErrorCode.ArchiefActieDatumError,
                "Besluit.vervaldatum moet gezet worden voordat de zaak kan worden afgesloten."
            )
        );

        return null;
    }
}
