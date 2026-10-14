using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Controllers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Http;
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
        var clientId = NewClientId();

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
    }

    [Fact]
    public async Task Client_without_autorisaties_and_without_all_authorizations_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, heeftAlleAutorisaties: false);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
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
    public async Task Client_authorized_for_another_scope_only_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Update));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
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
    [InlineData(HttpMethodNames.Post, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Put, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Patch, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Delete, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Post, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Put, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Patch, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Delete, Api.LatestVersion_1_1)]
    public async Task Client_authorized_for_the_read_scope_only_is_forbidden_to_write(string method, string apiVersion)
    {
        var clientId = NewClientId();
        var bodyClientId = NewClientId();
        var applicatie = await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Read));

        var response = await _client.SendAsync(AutorisatiesRequests.GetOrWrite(method, applicatie.Id, clientId, Rsin, bodyClientId, apiVersion));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
        var stored = await FindAsync(_factory, applicatie.Id);
        Assert.NotNull(stored);
        Assert.Equal(applicatie.Label, stored.Label);
        Assert.Null(await FindByClientIdAsync(_factory, bodyClientId));
    }

    [Theory]
    [InlineData(HttpMethodNames.Get, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Post, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Put, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Patch, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Delete, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Get, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Post, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Put, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Patch, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Delete, Api.LatestVersion_1_1)]
    public async Task Get_or_write_without_authorization_header_is_unauthorized(string method, string apiVersion)
    {
        var bodyClientId = NewClientId();
        var target = await SeedAsync(NewClientId(), heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(
            AutorisatiesRequests.GetOrWrite(method, target.Id, clientId: null, rsin: null, bodyClientId, apiVersion)
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var stored = await FindAsync(_factory, target.Id);
        Assert.NotNull(stored);
        Assert.Equal(target.Label, stored.Label);
        Assert.Null(await FindByClientIdAsync(_factory, bodyClientId));
    }

    [Theory]
    [InlineData(HttpMethodNames.Get, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Post, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Put, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Patch, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Delete, Api.LatestVersion_1_0)]
    [InlineData(HttpMethodNames.Get, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Post, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Put, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Patch, Api.LatestVersion_1_1)]
    [InlineData(HttpMethodNames.Delete, Api.LatestVersion_1_1)]
    public async Task Client_authorized_for_autorisaties_of_another_component_is_forbidden_to_get_or_write(string method, string apiVersion)
    {
        var clientId = NewClientId();
        var bodyClientId = NewClientId();
        var applicatie = await SeedAsync(
            clientId,
            autorisatie: Autorisatie(Rsin, Component.zrc, AuthorizationScopes.Autorisaties.Read, AuthorizationScopes.Autorisaties.Update)
        );

        var response = await _client.SendAsync(AutorisatiesRequests.GetOrWrite(method, applicatie.Id, clientId, Rsin, bodyClientId, apiVersion));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertNoConsumerLookup(_factory, clientId);
        var stored = await FindAsync(_factory, applicatie.Id);
        Assert.NotNull(stored);
        Assert.Equal(applicatie.Label, stored.Label);
        Assert.Null(await FindByClientIdAsync(_factory, bodyClientId));
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_0)]
    [InlineData(Api.LatestVersion_1_1)]
    public async Task Client_authorized_for_the_update_scope_can_create_an_applicatie_owned_by_its_organisation(string apiVersion)
    {
        var clientId = await SeedUpdateClientAsync();
        var bodyClientId = NewClientId();

        var response = await _client.SendAsync(AutorisatiesRequests.CreateApplicatie(clientId, Rsin, bodyClientId, apiVersion));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await FindByClientIdAsync(_factory, bodyClientId);
        Assert.NotNull(created);
        Assert.Equal(Rsin, created.Owner);
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_0)]
    [InlineData(Api.LatestVersion_1_1)]
    public async Task Client_authorized_for_the_update_scope_can_update(string apiVersion)
    {
        var clientId = await SeedUpdateClientAsync();
        var target = await SeedAsync(NewClientId(), heeftAlleAutorisaties: true);
        var bodyClientId = NewClientId();

        var response = await _client.SendAsync(AutorisatiesRequests.UpdateApplicatie(target.Id, clientId, Rsin, bodyClientId, apiVersion));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await FindAsync(_factory, target.Id);
        Assert.NotNull(stored);
        Assert.Equal(bodyClientId, stored.ClientIds.Single().ClientId);
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_0)]
    [InlineData(Api.LatestVersion_1_1)]
    public async Task Client_authorized_for_the_update_scope_can_partially_update(string apiVersion)
    {
        var clientId = await SeedUpdateClientAsync();
        var target = await SeedAsync(NewClientId(), heeftAlleAutorisaties: true);
        var label = NewLabel();

        var response = await _client.SendAsync(AutorisatiesRequests.PartialUpdateApplicatie(target.Id, clientId, Rsin, label, apiVersion));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await FindAsync(_factory, target.Id);
        Assert.NotNull(stored);
        Assert.Equal(label, stored.Label);
    }

    [Theory]
    [InlineData(Api.LatestVersion_1_0)]
    [InlineData(Api.LatestVersion_1_1)]
    public async Task Client_authorized_for_the_update_scope_can_delete(string apiVersion)
    {
        var clientId = await SeedUpdateClientAsync();
        var target = await SeedAsync(NewClientId(), heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.DeleteApplicatie(target.Id, clientId, Rsin, apiVersion));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await FindAsync(_factory, target.Id));
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

    private async Task<string> SeedUpdateClientAsync()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Rsin, Component.ac, AuthorizationScopes.Autorisaties.Update));

        return clientId;
    }

    private Task<Applicatie> SeedAsync(string clientId, bool heeftAlleAutorisaties = false, Autorisatie autorisatie = null)
    {
        return ApplicatieAsync(_factory, Rsin, clientId, heeftAlleAutorisaties, autorisatie);
    }
}
