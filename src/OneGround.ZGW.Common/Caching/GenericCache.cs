using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Caching;

/// <summary>
/// Normally this cache will be use within scoped context without any expiration
/// </summary>
/// <typeparam name="T">Generic object to be cached</typeparam>
public interface IGenericCache<T>
{
    Task<T> GetOrCacheAndGetAsync(string key, Func<Task<T>> factory);
}

/// <summary>
/// Normally this cache will be use within scoped context without any expiration.
/// Thread-safe (single-flight per key: concurrent callers for the same key share one factory
/// invocation instead of racing), so it can safely back resolvers invoked concurrently for
/// different entities within the same scope.
/// </summary>
/// <typeparam name="T">Generic object to be cached</typeparam>
public class GenericCache<T> : IGenericCache<T>
{
    private readonly ConcurrentDictionary<string, Lazy<Task<T>>> _cache = new();

    public async Task<T> GetOrCacheAndGetAsync(string key, Func<Task<T>> factory)
    {
        var lazy = _cache.GetOrAdd(key, _ => new Lazy<Task<T>>(factory));
        try
        {
            return await lazy.Value;
        }
        catch
        {
            // Note: Don't cache a failed attempt, so the next call retries instead of failing forever.
            _cache.TryRemove(KeyValuePair.Create(key, lazy));
            throw;
        }
    }
}
