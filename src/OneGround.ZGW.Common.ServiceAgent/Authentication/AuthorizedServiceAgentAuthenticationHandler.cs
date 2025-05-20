using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OneGround.ZGW.Common.ServiceAgent.Authentication;

public class AuthorizedServiceAgentAuthenticationHandler : DelegatingHandler
{
    private readonly IClientJwtTokenContext _clientJwtTokenContext;

    public AuthorizedServiceAgentAuthenticationHandler(IClientJwtTokenContext tokenContextAccessor)
    {
        _clientJwtTokenContext = tokenContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, _clientJwtTokenContext.Token);

        return await base.SendAsync(request, cancellationToken);
    }
}
