using System.Net.Http.Json;
using System.Text.Json.Serialization;
using KeycloakSetup.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;

namespace KeycloakSetup.Services;

public class KeycloakAccessTokenProvider : IAccessTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly KeycloakSettings _settings;
    private readonly ILogger<KeycloakAccessTokenProvider> _logger;
    private string? _cachedToken;
    private DateTime _tokenExpiryTime;
    
    public AllowedHostsValidator AllowedHostsValidator { get; }

    public KeycloakAccessTokenProvider(
        HttpClient httpClient,
        IOptions<KeycloakSettings> settings,
        ILogger<KeycloakAccessTokenProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        AllowedHostsValidator = new AllowedHostsValidator();
    }

    public async Task<string> GetAuthorizationTokenAsync(Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        // Check if we have a cached token that's still valid
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryTime)
        {
            return _cachedToken;
        }

        // Get a new token
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Failed to retrieve access token from Keycloak");
        }

        return token;
    }
    
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving access token from Keycloak admin API...");

            var tokenEndpoint = $"{_settings.BaseUrl}/realms/master/protocol/openid-connect/token";
            var formParams = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "password"),
                new("client_id", "admin-cli"),
                new("username", _settings.AdminUsername),
                new("password", _settings.AdminPassword)
            };

            var formContent = new FormUrlEncodedContent(formParams);
            var response = await _httpClient.PostAsync(tokenEndpoint, formContent, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken);

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _cachedToken = tokenResponse.AccessToken;
                    _tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 5);

                    _logger.LogInformation("Successfully retrieved access token from Keycloak");
                    return tokenResponse.AccessToken;
                }
            }

            _logger.LogError("Failed to retrieve access token: {StatusCode}", response.StatusCode);

            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving access token from Keycloak");
            return string.Empty;
        }
    }
}

public class AccessTokenResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")] public required string TokenType { get; set; }
}