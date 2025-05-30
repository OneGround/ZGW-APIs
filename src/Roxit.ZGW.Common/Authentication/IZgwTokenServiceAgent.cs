using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace Roxit.ZGW.Common.Authentication;

public interface IZgwTokenServiceAgent
{
    Task<TokenResponse> GetTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken);
}
