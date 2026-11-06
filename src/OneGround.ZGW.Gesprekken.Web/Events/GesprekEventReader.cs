using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneGround.ZGW.Gesprekken.DataModel;

namespace OneGround.ZGW.Gesprekken.Web.Events;

public class GesprekEventFilter
{
    public IReadOnlyCollection<Guid> Gesprekken { get; init; } = [];
    public string AanleidingZaak { get; init; }
}

public interface IGesprekEventReader
{
    /// <summary>The last sequence in the stream of the tenant, or 0 if it is empty.</summary>
    Task<long> GetHeadAsync(string rsin, CancellationToken cancellationToken);

    /// <summary>The first sequence still available, or null if the stream of the tenant is empty.</summary>
    Task<long?> GetOldestAsync(string rsin, CancellationToken cancellationToken);

    Task<List<GesprekEventRecord>> ReadAfterAsync(string rsin, long cursor, GesprekEventFilter filter, int max, CancellationToken cancellationToken);
}

public class GesprekEventReader : IGesprekEventReader
{
    private readonly GrcDbContext _context;

    public GesprekEventReader(GrcDbContext context)
    {
        _context = context;
    }

    public async Task<long> GetHeadAsync(string rsin, CancellationToken cancellationToken) =>
        await _context.Events.AsNoTracking().Where(e => e.Owner == rsin).MaxAsync(e => (long?)e.Sequence, cancellationToken) ?? 0;

    public Task<long?> GetOldestAsync(string rsin, CancellationToken cancellationToken) =>
        _context.Events.AsNoTracking().Where(e => e.Owner == rsin).MinAsync(e => (long?)e.Sequence, cancellationToken);

    public Task<List<GesprekEventRecord>> ReadAfterAsync(
        string rsin,
        long cursor,
        GesprekEventFilter filter,
        int max,
        CancellationToken cancellationToken
    )
    {
        // Redacted events keep their sequence but are not delivered: their content is gone
        var query = _context.Events.AsNoTracking().Where(e => e.Owner == rsin && e.Sequence > cursor && !e.Geredigeerd);

        if (filter.Gesprekken.Count > 0)
        {
            query = query.Where(e => filter.Gesprekken.Contains(e.GesprekId));
        }

        if (filter.AanleidingZaak != null)
        {
            query = query.Where(e => e.AanleidingZaak == filter.AanleidingZaak);
        }

        return query.OrderBy(e => e.Sequence).Take(max).ToListAsync(cancellationToken);
    }
}
