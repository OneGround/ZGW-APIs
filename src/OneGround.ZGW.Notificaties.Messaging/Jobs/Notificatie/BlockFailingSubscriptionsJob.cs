using System.Globalization;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Options;
using OneGround.ZGW.Notificaties.Messaging.Jobs.Filters;
using StackExchange.Redis;

namespace OneGround.ZGW.Notificaties.Messaging.Jobs.Notificatie;

public interface IBlockFailingSubscriptionsJob
{
    Task BlockFailingSubscriptionsAsync(PerformContext context = null);
}

[Queue(Constants.NrcListenerMainQueue)]
[RetryQueue(Constants.NrcListenerRetryQueue)]
public class BlockFailingSubscriptionsJob : IBlockFailingSubscriptionsJob
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IOptions<CircuitBreakerOptions> _circuitBreakerOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BlockFailingSubscriptionsJob> _logger;

    public BlockFailingSubscriptionsJob(
        IDistributedCache cache,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<CircuitBreakerOptions> circuitBreakerOptions,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BlockFailingSubscriptionsJob> logger
    )
    {
        _cache = cache;
        _connectionMultiplexer = connectionMultiplexer;
        _circuitBreakerOptions = circuitBreakerOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task BlockFailingSubscriptionsAsync(PerformContext context = null)
    {
        try
        {
            var expired = await GetExpiredFailingSubscriptionsAsync(CancellationToken.None);
            if (expired.Count == 0)
            {
                return;
            }

            var blocked = await BlockSubscriptionsAsync(expired, CancellationToken.None);

            foreach (var abonnementId in expired)
            {
                await _cache.RemoveAsync(CircuitBreakerCacheKeys.ForFailingSince(abonnementId), CancellationToken.None);
            }

            _logger.LogWarning(
                "Block-scan: blocked {BlockedCount} subscription(s) that were failing continuously for at least {BlockSubscriptionAfter}.",
                blocked,
                _circuitBreakerOptions.Value.BlockSubscriptionAfter
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Block-scan: failed to block expired failing subscriptions. Will retry on the next scheduled run.");
        }
    }

    private async Task<IReadOnlyList<Guid>> GetExpiredFailingSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var expired = new List<Guid>();

        var endpoint = _connectionMultiplexer.GetEndPoints()[0];
        IServer server = _connectionMultiplexer.GetServer(endpoint);

        foreach (var key in server.Keys(pattern: $"{CircuitBreakerCacheKeys.FailingSincePrefix}*"))
        {
            if (key == default(RedisKey))
            {
                continue;
            }

            var keyString = key.ToString();
            var suffix = keyString[CircuitBreakerCacheKeys.FailingSincePrefix.Length..];

            if (!Guid.TryParse(suffix, out var abonnementId))
            {
                _logger.LogWarning("Skipping failing-since marker with unparseable subscription id in key '{Key}'.", keyString);
                continue;
            }

            var value = await _cache.GetStringAsync(keyString, cancellationToken);
            if (value == null || !DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var failingSince))
            {
                _logger.LogWarning("Skipping failing-since marker '{Key}' with unparseable timestamp value '{Value}'.", keyString, value);
                continue;
            }

            if (DateTime.UtcNow - failingSince >= _circuitBreakerOptions.Value.BlockSubscriptionAfter)
            {
                expired.Add(abonnementId);
            }
        }

        return expired;
    }

    /// <summary>
    /// Marks the given subscriptions as blocked in the database. Uses a tracked load+save (NOT
    /// ExecuteUpdateAsync) so it works against the in-memory provider used in tests. Returns the
    /// number of subscriptions actually transitioned to blocked.
    /// </summary>
    // Public so the DB-block step can be unit-tested independently of the Redis keyspace enumeration.
    public async Task<int> BlockSubscriptionsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NrcDbContext>();

        var abonnementen = await dbContext.Abonnementen.Where(a => ids.Contains(a.Id) && !a.Blocked).ToListAsync(cancellationToken);

        foreach (var abonnement in abonnementen)
        {
            abonnement.Blocked = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return abonnementen.Count;
    }
}
