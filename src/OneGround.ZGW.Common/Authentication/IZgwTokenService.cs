using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace OneGround.ZGW.Common.Authentication;

public interface IZgwTokenService
{
    Task<TokenResponse> GetTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
}
