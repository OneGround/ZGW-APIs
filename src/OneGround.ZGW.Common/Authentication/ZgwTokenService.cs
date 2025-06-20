using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Exceptions;

namespace OneGround.ZGW.Common.Authentication;

public class ZgwTokenService : IZgwTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IZgwAuthDiscoveryCache _zgwAuthDiscoveryCache;

    public ZgwTokenService(IHttpClientFactory httpClientFactory, IZgwAuthDiscoveryCache zgwAuthDiscoveryCache)
    {
        _httpClient = httpClientFactory.CreateClient(ServiceRoleName.IDP);
        _zgwAuthDiscoveryCache = zgwAuthDiscoveryCache;
    }

    public async Task<TokenResponse> GetTokenAsync(string clientId, string clientSecret, CancellationToken cancellationToken)
    {
        var tokenEndpoint = await GetTokenEndpointAsync();

        var request = new ClientCredentialsTokenRequest
        {
            Address = tokenEndpoint,
            ClientId = clientId,
            ClientSecret = clientSecret,
            ClientCredentialStyle = ClientCredentialStyle.PostBody,
        };

        var response = await _httpClient.RequestClientCredentialsTokenAsync(request, cancellationToken);
        if (response.IsError)
            throw new OneGroundException(response.Error, response.Exception);

        return response;
    }

    private async Task<string> GetTokenEndpointAsync()
    {
        var discoveryDocumentResponse = await _zgwAuthDiscoveryCache.DiscoveryCache.GetAsync();
        if (discoveryDocumentResponse.IsError)
            throw new OneGroundException(discoveryDocumentResponse.Error, discoveryDocumentResponse.Exception);

        return discoveryDocumentResponse.TokenEndpoint;
    }
}
