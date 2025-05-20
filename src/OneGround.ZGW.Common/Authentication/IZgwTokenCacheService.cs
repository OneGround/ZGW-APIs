using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Authentication;

public interface IZgwTokenCacheService
{
    Task<string> GetCachedTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken = default);
}
