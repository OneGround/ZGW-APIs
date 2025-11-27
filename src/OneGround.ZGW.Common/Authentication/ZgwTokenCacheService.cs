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

    public async Task<(string token, TimeSpan expiration)> GetCachedTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default
    )
    {
        var cacheKey = $"ZGW-token-{clientId}";

        var tokenWithExpiration = await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                var tokenResponse = await _zgwTokenService.GetTokenAsync(clientId, clientSecret, cancellationToken);
                var safeExpiresIn = Math.Max(tokenResponse.ExpiresIn - 60, 1);
                var expiration = TimeSpan.FromSeconds(safeExpiresIn);

                entry.AbsoluteExpirationRelativeToNow = expiration;

                return (tokenResponse.AccessToken, expiration);
            }
        );

        return tokenWithExpiration;
    }
}
