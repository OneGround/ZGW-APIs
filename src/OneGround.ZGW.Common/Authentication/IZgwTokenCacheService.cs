using System;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Authentication;

public interface IZgwTokenCacheService
{
    Task<(string token, TimeSpan expiration)> GetCachedTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default
    );
}
