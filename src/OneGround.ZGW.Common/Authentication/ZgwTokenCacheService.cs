using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace OneGround.ZGW.Common.Authentication;

public class ZgwTokenCacheService : IZgwTokenCacheService
{
    private readonly IZgwTokenService _zgwTokenService;
    private readonly IMemoryCache _memoryCache;

    public ZgwTokenCacheService(IZgwTokenService zgwTokenService, IMemoryCache memoryCache)
    {
        _zgwTokenService = zgwTokenService;
        _memoryCache = memoryCache;
    }

    public async Task<string> GetCachedTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"ZGW-token-{clientId}";

        var token = await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                var tokenResponse = await _zgwTokenService.GetTokenAsync(clientId, clientSecret, cancellationToken);

                var expiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60);
                entry.AbsoluteExpirationRelativeToNow = expiration;

                return tokenResponse.AccessToken;
            }
        );

        return token;
    }
}
