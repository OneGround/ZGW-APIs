using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Authentication and scope checks on GET /zaken, run by the API's own [Authorize] attribute, scope filter and authorization resolver.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZakenAuthorizationTests
{
    private const string Rsin = TestRsins.A;

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
        _factory.AutorisatiesApi.GrantAllAuthorizations(clientId);

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, rsin: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(LookupsOf(clientId));
    }

    [Fact]
    public async Task Client_the_autorisaties_api_does_not_know_is_forbidden()
    {
        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(NewClientId(), Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_without_authorizations_is_forbidden()
    {
        var clientId = NewClientId();
        _factory.AutorisatiesApi.GrantNoAuthorizations(clientId);

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_for_another_scope_only_is_forbidden()
    {
        var clientId = NewClientId();
        _factory.AutorisatiesApi.Grant(clientId, Autorisatie(AuthorizationScopes.Zaken.Create));

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_to_read_zaken_of_a_zaaktype_gets_ok()
    {
        var clientId = NewClientId();
        _factory.AutorisatiesApi.Grant(clientId, Autorisatie(AuthorizationScopes.Zaken.Read));

        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(LookupsOf(clientId));
    }

    private HttpRequestMessage[] LookupsOf(string clientId)
    {
        return _factory.OutboundHttp.Requests.Where(r => r.RequestUri!.Query.Contains($"clientId={clientId}")).ToArray();
    }

    private static AutorisatieResponseDto Autorisatie(string scope)
    {
        return new AutorisatieResponseDto
        {
            Component = "zrc",
            Scopes = [scope],
            ZaakType = ZaakTypes.A,
            MaxVertrouwelijkheidaanduiding = nameof(VertrouwelijkheidAanduiding.zeer_geheim),
        };
    }

    private static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
