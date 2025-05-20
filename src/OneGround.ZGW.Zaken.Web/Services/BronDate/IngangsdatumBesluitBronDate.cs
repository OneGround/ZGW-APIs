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

public class IngangsdatumBesluitBronDate : BesluitBronDate, IBronDateService
{
    private readonly ZrcDbContext _context;

    public IngangsdatumBesluitBronDate(IBesluitenServiceAgent besluitenServiceAgent, ZrcDbContext context)
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

        var maxDate = default(DateOnly?);
        foreach (var zaakBesluitUrl in zaakBesluiten.Select(b => b.Besluit))
        {
            var besluit = await GetBesluitAsync(zaakBesluitUrl, errors);
            var date = DateOnly.Parse(besluit.IngangsDatum);
            if (!maxDate.HasValue || date > maxDate)
            {
                maxDate = date;
            }
        }

        return maxDate;
    }
}
