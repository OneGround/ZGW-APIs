using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;

namespace Roxit.ZGW.Catalogi.Web.Services;

public interface IEindStatusResolver
{
    Task ResolveAsync(StatusType statusType, CancellationToken cancellationToken);
    Task ResolveAsync(IList<StatusType> statusTypes, CancellationToken cancellationToken);
}

public class EindStatusResolver : IEindStatusResolver
{
    private readonly ILogger<EindStatusResolver> _logger;
    private readonly ZtcDbContext _context;

    public EindStatusResolver(ILogger<EindStatusResolver> logger, ZtcDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task ResolveAsync(StatusType statusType, CancellationToken cancellationToken)
    {
        var values = await GetStatusTypeMaximumVolgNummerAsync([statusType.ZaakTypeId], cancellationToken);

        ResolveIsEindStatus(values, statusType);
    }

    public async Task ResolveAsync(IList<StatusType> statusTypes, CancellationToken cancellationToken)
    {
        var zaakTypeIds = statusTypes.Select(s => s.ZaakTypeId);
        var values = await GetStatusTypeMaximumVolgNummerAsync(zaakTypeIds, cancellationToken);
        foreach (var statusType in statusTypes)
        {
            ResolveIsEindStatus(values, statusType);
        }
    }

    private void ResolveIsEindStatus(IReadOnlyDictionary<Guid, int> values, StatusType statusType)
    {
        var zaakTypeId = statusType.ZaakTypeId;
        if (values.TryGetValue(zaakTypeId, out var maxVolgNummer))
        {
            statusType.IsEindStatus = statusType.VolgNummer >= maxVolgNummer;
        }
        else
        {
            _logger.LogWarning("StatusType with ZaakType: {Uuid} does not exist. Cannot resolve maximum VolgNummer.", zaakTypeId);
        }
    }

    private async Task<IReadOnlyDictionary<Guid, int>> GetStatusTypeMaximumVolgNummerAsync(
        IEnumerable<Guid> zaakTypeIds,
        CancellationToken cancellationToken
    )
    {
        return await _context
            .StatusTypen.Where(s => zaakTypeIds.Contains(s.ZaakTypeId))
            .GroupBy(s => s.ZaakTypeId)
            .Select(g => new { ZaakTypeId = g.Key, MaxVolgNummer = g.Max(s => s.VolgNummer) })
            .ToDictionaryAsync(k => k.ZaakTypeId, v => v.MaxVolgNummer, cancellationToken);
    }
}
