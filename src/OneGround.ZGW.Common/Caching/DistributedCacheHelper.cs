using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Caching;

/// <summary>
/// This cache is a wrapper over the Microsoft IDistributedCache cache with an easy to use interface.
/// </summary>
public interface IDistributedCacheHelper
{
    /// <summary>
    /// Get the object instance of type <see cref="T"/> identified by <see cref="key"/> from cache. If not present factory delegate is used to get the data from the source.
    /// </summary>
    /// <typeparam name="T">Generic object to be cached</typeparam>
    /// <param name="key">The unique key to identify the cache entry</param>
    /// <param name="factory">Asynchronous (awaitable) Task factory delegate to get the data if not present in cache</param>
    /// <param name="absoluteExpirationRelativeToNow">The lifetime of the cache entry</param>
    /// <param name="cancellationToken">Task cancellation token</param>
    /// <returns>The value from the cache (or value from the factory delegate)</returns>
    Task<T> GetAsync<T>(string key, Func<Task<T>> factory, TimeSpan absoluteExpirationRelativeToNow, CancellationToken cancellationToken = default);
}

public class DistributedCacheHelper : IDistributedCacheHelper
{
    private readonly IDistributedCache _cache;

    public DistributedCacheHelper(IDistributedCache cache)
    {
        _cache = cache;
    }

    public Task<T> GetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default
    )
    {
        return GetFromCacheAsync(key, factory, absoluteExpirationRelativeToNow, cancellationToken);
    }

    private async Task<T> GetFromCacheAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken
    )
    {
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (cached != null)
        {
            return JsonConvert.DeserializeObject<T>(cached);
        }

        var result = await factory() ?? throw new NullReferenceException($"The result from {nameof(factory)} delegate is null.");
        var serialized = JsonConvert.SerializeObject(result);

        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow };
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);

        return result;
    }
}
