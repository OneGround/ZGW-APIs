using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace OneGround.ZGW.Common.Authentication;

public class ZgwTokenServiceAgent : IZgwTokenServiceAgent
{
    private readonly HttpClient _httpClient;
    private readonly IZgwAuthDiscoveryCache _zgwAuthDiscoveryCache;

    public ZgwTokenServiceAgent(HttpClient httpClient, IZgwAuthDiscoveryCache zgwAuthDiscoveryCache)
    {
        _httpClient = httpClient;
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
            throw new Exception(response.Error, response.Exception);

        return response;
    }

    private async Task<string> GetTokenEndpointAsync()
    {
        var discoveryDocumentResponse = await _zgwAuthDiscoveryCache.DiscoveryCache.GetAsync();
        if (discoveryDocumentResponse.IsError)
            throw new Exception(discoveryDocumentResponse.Error, discoveryDocumentResponse.Exception);

        return discoveryDocumentResponse.TokenEndpoint;
    }
}
