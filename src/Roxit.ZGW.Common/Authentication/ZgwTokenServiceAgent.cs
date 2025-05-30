using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using Microsoft.Extensions.Options;

namespace Roxit.ZGW.Common.Authentication;

public class ZgwTokenServiceAgent : IZgwTokenServiceAgent
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<ZgwAuthConfiguration> _options;

    public ZgwTokenServiceAgent(HttpClient httpClient, IOptions<ZgwAuthConfiguration> options)
    {
        _httpClient = httpClient;
        _options = options;
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
            throw new Exception(response.Error);

        return response;
    }

    private async Task<string> GetTokenEndpointAsync()
    {
        var discoveryPolicy = new DiscoveryPolicy { RequireKeySet = false };
        var discoveryCache = new DiscoveryCache(_options.Value.ZgwLegacyAuthProviderUrl, discoveryPolicy);

        var response = await discoveryCache.GetAsync();
        if (response.IsError)
            throw new Exception(response.Error);

        return response.TokenEndpoint;
    }
}
