using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Roxit.ZGW.Common.Authentication;

public class ZgwTokenCacheService : IZgwTokenCacheService
{
    private readonly IZgwTokenServiceAgent _zgwTokenServiceAgent;
    private readonly IMemoryCache _memoryCache;

    public ZgwTokenCacheService(IZgwTokenServiceAgent zgwTokenServiceAgent, IMemoryCache memoryCache)
    {
        _zgwTokenServiceAgent = zgwTokenServiceAgent;
        _memoryCache = memoryCache;
    }

    public async Task<string> GetCachedTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ZGW-token-{clientId}";

        var token = await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                var tokenResponse = await _zgwTokenServiceAgent.GetTokenAsync(clientId, clientSecret, cancellationToken);

                var expiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60);
                entry.AbsoluteExpirationRelativeToNow = expiration;

                return tokenResponse.AccessToken;
            }
        );

        return token;
    }
}
