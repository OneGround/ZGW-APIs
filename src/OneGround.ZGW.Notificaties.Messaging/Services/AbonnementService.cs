using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.Configuration;

namespace OneGround.ZGW.Notificaties.Messaging.Services;

public interface IAbonnementService
{
    Task<Abonnement> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

public class AbonnementService(
    IMemoryCache memoryCache,
    IOptions<ApplicationOptions> applicationOptions,
    NrcDbContext dbContext,
    ILogger<AbonnementService> logger
) : IAbonnementService
{
    public async Task<Abonnement> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var abonnementen = await memoryCache.GetOrCreateAsync(
            $"{nameof(AbonnementService)}_abonnementen",
            async e =>
            {
                e.AbsoluteExpirationRelativeToNow = applicationOptions.Value.AbonnementenCacheExpirationTime;

                var q = await dbContext.Abonnementen.AsNoTracking().ToDictionaryAsync(k => k.Id, v => v, cancellationToken);

                logger.LogDebug("{Count} abonnementen retrieved and all cached", q.Count);

                return q;
            }
        );

        return abonnementen[id];
    }
}
