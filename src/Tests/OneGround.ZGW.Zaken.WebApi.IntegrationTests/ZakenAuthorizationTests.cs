using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Authentication and scope checks on GET /zaken. These only pass when the API's own [Authorize] attribute and
/// scope filter run: the test host substitutes the authentication scheme and the authorization resolver, nothing else.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZakenAuthorizationTests
{
    private const string Rsin = "111222333";

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZakenAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Request_without_authorization_header_is_unauthorized()
    {
        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId: null, rsin: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Client_without_rsin_claim_is_unauthorized()
    {
        var clientId = NewClientId();
        _factory.AuthorizationResolver.GrantAllAuthorizations(clientId);

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, rsin: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Denied by the scope filter's rsin check, before it resolves the client's authorizations
        Assert.DoesNotContain(_factory.AuthorizationResolver.Resolutions, r => r.ClientId == clientId);
    }

    [Fact]
    public async Task Client_the_resolver_finds_no_application_for_is_forbidden()
    {
        var clientId = NewClientId();
        _factory.AuthorizationResolver.ResolveNothingFor(clientId);

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_without_authorizations_is_forbidden()
    {
        var clientId = NewClientId();
        _factory.AuthorizationResolver.GrantNoAuthorizations(clientId);

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_to_read_zaken_of_a_zaaktype_gets_ok()
    {
        var clientId = NewClientId();
        _factory.AuthorizationResolver.GrantPermissions(
            clientId,
            new AuthorizationPermission
            {
                ZaakType = ZaakTypes.A,
                Scopes = [AuthorizationScopes.Zaken.Read],
                MaximumVertrouwelijkheidAanduiding = (int)VertrouwelijkheidAanduiding.zeer_geheim,
            }
        );

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The API's scope filter asked the resolver for this client, for the Zaken component and the read scope
        var resolution = Assert.Single(_factory.AuthorizationResolver.Resolutions, r => r.ClientId == clientId);
        Assert.Equal(ServiceRoleName.ZRC, resolution.Component);
        Assert.Equal([AuthorizationScopes.Zaken.Read], resolution.Scopes);
    }

    private static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
