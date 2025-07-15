using System.Text.Json;
using KeycloakSetup.Client;
using KeycloakSetup.Client.Models;
using KeycloakSetup.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace KeycloakSetup.Services
{
    public class KeycloakSetupService
    {
        private readonly KeycloakClient _keycloakClient;
        private readonly KeycloakSettings _settings;
        private readonly ILogger<KeycloakSetupService> _logger;

        public KeycloakSetupService(
            IOptions<KeycloakSettings> settings,
            ILogger<KeycloakSetupService> logger,
            KeycloakAccessTokenProvider tokenProvider,
            HttpClient httpClient)
        {
            _settings = settings.Value;
            _logger = logger;
            _keycloakClient = CreateKeycloakClient(tokenProvider, httpClient);
        }

        public async Task<bool> SetupKeycloakAsync()
        {
            try
            {
                _logger.LogInformation("Starting Keycloak setup for realm: {RealmName}", _settings.RealmName);

                // Step 1: Create realm
                if (!await CreateRealmAsync())
                {
                    _logger.LogError("Failed to create realm");
                    return false;
                }

                // Step 2: Create clients
                if (!await CreateClientsAsync())
                {
                    _logger.LogError("Failed to create clients");
                    return false;
                }
                
                // Step 3: Configure RSIN claim mappers
                if (!await ConfigureRsinClaimMappersAsync())
                {
                    _logger.LogError("Failed to configure RSIN claim mappers");
                    return false;
                }

                _logger.LogInformation("Keycloak setup completed successfully for realm: {RealmName}",
                    _settings.RealmName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Keycloak setup");
                return false;
            }
        }

        private KeycloakClient CreateKeycloakClient(KeycloakAccessTokenProvider tokenProvider, HttpClient httpClient)
        {
            var requestAdapter = new HttpClientRequestAdapter(new BaseBearerTokenAuthenticationProvider(tokenProvider),
                httpClient: httpClient)
            {
                BaseUrl = _settings.BaseUrl
            };

            return new KeycloakClient(requestAdapter);
        }

        private async Task<bool> CreateRealmAsync()
        {
            _logger.LogInformation("Creating realm: {RealmName}", _settings.RealmName);

            try
            {
                var existingRealm = await _keycloakClient.Admin.Realms[_settings.RealmName].GetAsync();
                if (existingRealm != null)
                {
                    _logger.LogInformation("Realm {RealmName} already exists, skipping creation", _settings.RealmName);
                    return true;
                }
            }
            catch (Exception)
            {
                // Realm doesn't exist, continue with creation
                _logger.LogDebug("Realm {RealmName} doesn't exist, will create it", _settings.RealmName);
            }

            // using an anonymous type, because with RealmRepresentation request fails with "unable to read contents from stream" error
            var realm = new
            {
                Realm = _settings.RealmName,
                Enabled = true
            };

            var realmJson = JsonSerializer.Serialize(realm, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var realmStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(realmJson));
            
            try
            {
                await _keycloakClient.Admin.Realms.PostAsync(realmStream);

                _logger.LogInformation("Successfully created realm: {RealmName}", _settings.RealmName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating realm");
                return false;
            }
        }

        private async Task<bool> CreateClientsAsync()
        {
            try
            {
                _logger.LogInformation("Creating {ClientCount} clients", _settings.Clients.Count);

                // Get existing clients once
                var existingClients = await _keycloakClient.Admin.Realms[_settings.RealmName].Clients.GetAsync();

                foreach (var clientConfig in _settings.Clients)
                {
                    _logger.LogInformation("Creating client: {ClientName} with RSIN: {Rsin}", clientConfig.ClientName, clientConfig.Rsin);

                    // Check if client already exists
                    var existingClient = existingClients?.FirstOrDefault(c => c.ClientId == clientConfig.ClientId);

                    if (existingClient != null)
                    {
                        _logger.LogInformation("Client {ClientId} already exists, skipping creation", clientConfig.ClientId);
                        continue;
                    }

                    // Create the client
                    var client = new ClientRepresentation
                    {
                        ClientId = clientConfig.ClientId,
                        Name = clientConfig.ClientName,
                        Description = clientConfig.ClientDescription,
                        Enabled = true,
                        ServiceAccountsEnabled = true,
                        Protocol = "openid-connect",
                        ClientAuthenticatorType = "client-secret",
                        PublicClient = false
                    };

                    await _keycloakClient.Admin.Realms[_settings.RealmName].Clients.PostAsync(client);

                    _logger.LogInformation("Successfully created client: {ClientName} with RSIN: {Rsin}", clientConfig.ClientName, clientConfig.Rsin);
                }

                _logger.LogInformation("Successfully created all clients");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating clients");
                return false;
            }
        }

        private async Task<bool> ConfigureRsinClaimMappersAsync()
        {
            try
            {
                _logger.LogInformation("Configuring RSIN claim mappers...");

                var existingClients = await _keycloakClient.Admin.Realms[_settings.RealmName].Clients.GetAsync();

                foreach (var clientConfig in _settings.Clients)
                {
                    var keycloakClient = existingClients?.FirstOrDefault(c => c.ClientId == clientConfig.ClientId);
                    if (keycloakClient == null)
                    {
                        _logger.LogWarning("Client {ClientId} not found, skipping RSIN claim mapper configuration", clientConfig.ClientId);
                        continue;
                    }

                    // Check if RSIN claim mapper already exists on the client
                    var existingMappers = await _keycloakClient.Admin.Realms[_settings.RealmName].Clients[keycloakClient.Id].ProtocolMappers.Models.GetAsync();
                    var rsinMapperName = $"rsin-{clientConfig.ClientId}";
                    var existingRsinMapper = existingMappers?.FirstOrDefault(m => m.Name == rsinMapperName);

                    if (existingRsinMapper != null)
                    {
                        _logger.LogInformation("RSIN claim mapper {MapperName} already exists for client {ClientId}, skipping creation", rsinMapperName, clientConfig.ClientId);
                        continue;
                    }

                    // Create hardcoded claim mapper for RSIN directly on the client
                    var rsinClaimMapper = new ProtocolMapperRepresentation
                    {
                        Name = rsinMapperName,
                        Protocol = "openid-connect",
                        ProtocolMapper = "oidc-hardcoded-claim-mapper",
                        Config = new ProtocolMapperRepresentation_config
                        {
                            AdditionalData = new Dictionary<string, object>
                            {
                                { "claim.name", "rsin" },
                                { "claim.value", clientConfig.Rsin },
                                { "jsonType.label", "String" },
                                { "id.token.claim", "true" },
                                { "access.token.claim", "true" },
                                { "userinfo.token.claim", "true" }
                            }
                        }
                    };

                    await _keycloakClient.Admin.Realms[_settings.RealmName].Clients[keycloakClient.Id].ProtocolMappers.Models.PostAsync(rsinClaimMapper);
                    _logger.LogInformation("Created RSIN claim mapper {MapperName} with value {Rsin} for client {ClientId}", rsinMapperName, clientConfig.Rsin, clientConfig.ClientId);
                }

                _logger.LogInformation("Successfully configured RSIN claim mappers");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring RSIN claim mappers");
                return false;
            }
        }
    }
}