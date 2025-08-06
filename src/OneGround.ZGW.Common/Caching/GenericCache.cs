using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Caching;

/// <summary>
/// Normally this cache will be use within scoped context without any expiration
/// </summary>
/// <typeparam name="T">Generic object to be cached</typeparam>
public interface IGenericCache<T>
{
    T GetOrCacheAndGet(string key, Func<T> factory);
    Task<T> GetOrCacheAndGetAsync(string key, Func<Task<T>> factory);
}

/// <summary>
/// Normally this cache will be use within scoped context without any expiration
/// </summary>
/// <typeparam name="T">Generic object to be cached</typeparam>
public class GenericCache<T> : IGenericCache<T>
{
    private readonly Dictionary<string, T> _cache = [];

    public T GetOrCacheAndGet(string key, Func<T> factory)
    {
        if (!_cache.TryGetValue(key, out var value))
        {
            value = factory();
            _cache.Add(key, value);
        }
        return value;
    }

    public async Task<T> GetOrCacheAndGetAsync(string key, Func<Task<T>> factory)
    {
        if (!_cache.TryGetValue(key, out var value))
        {
            value = await factory();
            _cache.Add(key, value);
        }
        return value;
    }
}
