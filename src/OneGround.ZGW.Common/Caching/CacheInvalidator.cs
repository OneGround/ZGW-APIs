using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace OneGround.ZGW.Common.Caching;

public interface ICacheInvalidator
{
    Task<bool> InvalidateAsync(CacheEntity entity, Guid id, string rsin);
    Task<long> InvalidateAsync(CacheEntity entity, IList<Guid> ids, string rsin);
    Task<IEnumerable<string>> InvalidateByServiceAsync(string service);
    Task<IEnumerable<string>> InvalidateAllAsync();
    Task<long> InvalidateAsync(CacheEntity entity, IList<string> ids);
}

public class CacheInvalidator : ICacheInvalidator
{
    private const int REDIS_PAGE_SIZE = 1000;

    private readonly ILogger<CacheInvalidator> _logger;
    private readonly IConnectionMultiplexer _redisConnection;

    public CacheInvalidator(ILogger<CacheInvalidator> logger, IConnectionMultiplexer redisConnection)
    {
        _logger = logger;
        _redisConnection = redisConnection;
    }

    public async Task<long> InvalidateAsync(CacheEntity entity, IList<string> ids)
    {
        var patterns = ids.Select(id => $"ZGW:{entity.Service}:{entity.Entity}:{id}:*").ToArray();

        if (patterns.Length > 0)
        {
            _logger.LogDebug("Removing {Ids} in {Entity} from response cache.", string.Join(',', ids), entity);
        }

        var keysRemoved = await InvalidatePatternsAsync(patterns);

        return keysRemoved.Count();
    }

    public async Task<bool> InvalidateAsync(CacheEntity entity, Guid id, string rsin)
    {
        var pattern = $"ZGW:{entity.Service}:{entity.Entity}:{rsin}:{id}:*";

        var keysRemoved = await InvalidatePatternsAsync(pattern);

        return keysRemoved.Any();
    }

    public async Task<long> InvalidateAsync(CacheEntity entity, IList<Guid> ids, string rsin)
    {
        var patterns = ids.Select(id => $"ZGW:{entity.Service}:{entity.Entity}:{rsin}:{id}:*").ToArray();

        if (patterns.Length > 0)
        {
            _logger.LogDebug("Removing {Ids} in {Entity}:{Rsin} from response cache.", string.Join(',', ids), entity, rsin);
        }

        var keysRemoved = await InvalidatePatternsAsync(patterns);

        return keysRemoved.Count();
    }

    public Task<IEnumerable<string>> InvalidateByServiceAsync(string service)
    {
        ArgumentException.ThrowIfNullOrEmpty(service);

        return InvalidatePatternsAsync($"ZGW:{service}:*");
    }

    public Task<IEnumerable<string>> InvalidateAllAsync()
    {
        return InvalidatePatternsAsync("ZGW:*");
    }

    private async Task<IEnumerable<string>> InvalidatePatternsAsync(params string[] patterns)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            var removedKeys = new List<string>();

            var endpoints = _redisConnection.GetEndPoints();

            for (int endpointIndex = 0; endpointIndex < endpoints.Length; endpointIndex++)
            {
                try
                {
                    var server = _redisConnection.GetServer(endpoints[endpointIndex]);

                    foreach (var pattern in patterns)
                    {
                        _logger.LogDebug("Invalidating {pattern} from redis cache in server[{index}]...", pattern, endpointIndex);

                        var scanResult = server.Keys(pattern: pattern, pageSize: REDIS_PAGE_SIZE);
                        var scanResultEnumerator = scanResult.GetEnumerator();

                        var isCompleted = false;
                        do
                        {
                            var keysToDelete = new List<RedisKey>();
                            for (var i = 0; i < REDIS_PAGE_SIZE; i++)
                            {
                                if (!scanResultEnumerator.MoveNext())
                                {
                                    isCompleted = true;
                                    break;
                                }
                                keysToDelete.Add(scanResultEnumerator.Current);
                            }

                            if (keysToDelete.Count > 0)
                            {
                                var db = _redisConnection.GetDatabase();
                                await db.KeyDeleteAsync(keysToDelete.ToArray());
                                removedKeys.AddRange(keysToDelete.Select(k => (string)k));
                            }
                        } while (!isCompleted);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invalidating cache entrie(s) on server {ep}", endpoints[endpointIndex]);
                    // Note: Continue invalidating on next server
                }
            }

            sw.Stop();
            _logger.LogDebug(
                "Redis cache invalidation completed after {time}ms. Total removed {count} keys.",
                sw.ElapsedMilliseconds,
                removedKeys.Count
            );

            return removedKeys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache entrie(s)");
            return [];
        }
    }
}
