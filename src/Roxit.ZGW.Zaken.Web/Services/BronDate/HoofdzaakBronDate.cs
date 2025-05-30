using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Roxit.ZGW.Zaken.DataModel;

namespace Roxit.ZGW.Zaken.Web.Services.BronDate;

public class HoofdzaakBronDate : IBronDateService
{
    private readonly ZrcDbContext _context;

    public HoofdzaakBronDate(ZrcDbContext context)
    {
        _context = context;
    }

    public async Task<DateOnly?> GetAsync(Zaak zaak, List<ArchiveValidationError> _, CancellationToken cancellationToken)
    {
        var hoofdzaak = await _context.Zaken.AsNoTracking().Include(z => z.Hoofdzaak).SingleAsync(z => z.Id == zaak.Id, cancellationToken);

        return hoofdzaak.Hoofdzaak?.Einddatum;
    }
}
