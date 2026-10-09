using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Controllers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;
using static OneGround.ZGW.Autorisaties.WebApi.IntegrationTests.AutorisatiesSeed;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Authentication and scope checks on /applicaties, resolved by the API's own database authorization resolver.
/// </summary>
[Collection(AutorisatiesApiCollection.Name)]
public class ApplicatieAuthorizationTests
{
    private const string Rsin = TestRsins.A;

    private readonly AutorisatiesWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApplicatieAuthorizationTests(AutorisatiesApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Request_without_authorization_header_is_unauthorized()
    {
        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId: null, rsin: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Client_without_rsin_claim_is_unauthorized()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, rsin: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_without_client_id_claim_is_unauthorized()
    {
        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId: null, Rsin));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Client_in_no_applicatie_is_forbidden()
    {
        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(NewClientId(), Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_without_autorisaties_and_without_all_authorizations_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, heeftAlleAutorisaties: false);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_with_all_authorizations_and_no_autorisaties_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Fact]
    public async Task Client_with_all_authorizations_gets_ok_on_api_version_1_1()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin, Api.LatestVersion_1_1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Fact]
    public async Task Client_authorized_to_read_autorisaties_of_component_ac_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Read));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Fact]
    public async Task Client_authorized_to_read_autorisaties_of_another_component_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.zrc, AuthorizationScopes.Autorisaties.Read));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_for_another_scope_only_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Update));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_for_the_read_scope_among_other_scopes_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(
            clientId,
            autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Update, AuthorizationScopes.Autorisaties.Read)
        );

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Client_authorized_for_the_read_scope_only_is_forbidden_to_write(string method)
    {
        var clientId = NewClientId();
        var applicatie = await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Read));

        var response = await _client.SendAsync(AutorisatiesRequests.Write(method, applicatie.Id, clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var stored = await FindAsync(_factory, applicatie.Id);
        Assert.NotNull(stored);
        Assert.Equal(applicatie.Label, stored.Label);
    }

    [Fact]
    public async Task Client_authorized_for_the_update_scope_can_write()
    {
        var clientId = NewClientId();
        var applicatie = await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Update));

        var response = await _client.SendAsync(AutorisatiesRequests.PartialUpdateApplicatie(applicatie.Id, clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await FindAsync(_factory, applicatie.Id);
        Assert.NotEqual(applicatie.Label, stored.Label);
    }

    [Fact]
    public async Task Client_id_stored_in_another_letter_case_than_the_token_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId.ToUpperInvariant(), heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    private Task<Applicatie> SeedAsync(string clientId, bool heeftAlleAutorisaties = false, Autorisatie autorisatie = null)
    {
        return ApplicatieAsync(_factory, Rsin, clientId, heeftAlleAutorisaties, autorisatie);
    }
}
