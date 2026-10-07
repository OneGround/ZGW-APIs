using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Authentication and scope checks on GET /applicaties, resolved by the API's own database authorization resolver.
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
        AssertNoConsumerLookup(clientId);
    }

    [Fact]
    public async Task Client_authorized_to_read_autorisaties_of_component_ac_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Component.ac, AuthorizationScopes.Autorisaties.Read));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(clientId);
    }

    [Fact]
    public async Task Client_authorized_to_read_autorisaties_of_another_component_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Component.zrc, AuthorizationScopes.Autorisaties.Read));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_for_another_scope_only_is_forbidden()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId, autorisatie: Autorisatie(Component.ac, AuthorizationScopes.Autorisaties.Update));

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_authorized_for_the_read_scope_among_other_scopes_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(
            clientId,
            autorisatie: Autorisatie(Component.ac, AuthorizationScopes.Autorisaties.Update, AuthorizationScopes.Autorisaties.Read)
        );

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(clientId);
    }

    [Fact]
    public async Task Client_id_stored_in_another_letter_case_than_the_token_gets_ok()
    {
        var clientId = NewClientId();
        await SeedAsync(clientId.ToUpperInvariant(), heeftAlleAutorisaties: true);

        var response = await _client.SendAsync(AutorisatiesRequests.GetAllApplicaties(clientId, Rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertNoConsumerLookup(clientId);
    }

    private void AssertNoConsumerLookup(string clientId)
    {
        Assert.DoesNotContain(
            _factory.OutboundHttp.Requests,
            r =>
                r.RequestUri!.AbsolutePath.EndsWith("/applicaties/consumer", StringComparison.OrdinalIgnoreCase)
                && r.RequestUri.Query.Contains($"clientId={clientId}", StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task SeedAsync(string clientId, bool heeftAlleAutorisaties = false, Autorisatie autorisatie = null)
    {
        var applicatieId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcDbContext>();

        context.Applicaties.Add(
            new Applicatie
            {
                Id = applicatieId,
                Owner = Rsin,
                Label = $"integration-test-{applicatieId:N}",
                CreationTime = DateTime.UtcNow,
                HeeftAlleAutorisaties = heeftAlleAutorisaties,
                ClientIds =
                [
                    new ApplicatieClient
                    {
                        Id = Guid.NewGuid(),
                        ClientId = clientId,
                        CreationTime = DateTime.UtcNow,
                    },
                ],
                Autorisaties = autorisatie == null ? [] : [autorisatie],
            }
        );

        await context.SaveChangesAsync();
    }

    private static Autorisatie Autorisatie(Component component, params string[] scopes)
    {
        return new Autorisatie
        {
            Id = Guid.NewGuid(),
            Owner = Rsin,
            Component = component,
            Scopes = scopes,
        };
    }

    private static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
