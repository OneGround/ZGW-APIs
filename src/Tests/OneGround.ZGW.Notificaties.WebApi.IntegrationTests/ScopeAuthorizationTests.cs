using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Authentication and scope checks on the ten <c>[Scope]</c>-carrying actions of <see cref="ScopeMatrix"/>, resolved against the stub Autorisaties API.
/// </summary>
[Collection(NotificatiesApiCollection.Name)]
public class ScopeAuthorizationTests
{
    private const string Rsin = TestRsins.A;

    private readonly NotificatiesWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ScopeAuthorizationTests(NotificatiesApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    public static IEnumerable<object[]> Rows() => ScopeMatrix.Rows.Select(r => new object[] { r.Name });

    public static IEnumerable<object[]> RowsDeniedBySomeScope() => ScopeMatrix.RowsDeniedBySomeScope.Select(r => new object[] { r.Name });

    public static IEnumerable<object[]> AcceptedScopes() => ScopeMatrix.Rows.SelectMany(r => r.Scopes.Select(s => new object[] { r.Name, s }));

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Request_without_client_id_claim_is_unauthorized(string row)
    {
        var response = await SendAsync(row, clientId: null, rsin: Rsin);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, await NotificatiesRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Request_without_rsin_claim_is_unauthorized(string row)
    {
        var response = await SendAsync(row, NrcClients.WithAllAuthorizations(_factory), rsin: null);

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, await NotificatiesRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Client_unknown_to_the_autorisaties_api_is_forbidden(string row)
    {
        var response = await SendAsync(row, NrcClients.NewClientId(), Rsin);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await NotificatiesRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Client_without_autorisaties_and_without_all_authorizations_is_forbidden(string row)
    {
        var response = await SendAsync(row, NrcClients.WithoutAuthorizations(_factory), Rsin);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await NotificatiesRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Client_with_both_scopes_on_another_component_is_forbidden(string row)
    {
        var response = await SendAsync(row, NrcClients.AuthorizedOnAnotherComponent(_factory), Rsin);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await NotificatiesRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(RowsDeniedBySomeScope))]
    public async Task Client_with_every_scope_except_the_actions_is_forbidden(string row)
    {
        var matrixRow = ScopeMatrix.Named(row);
        var clientId = NrcClients.AuthorizedForAllScopesExcept(_factory, matrixRow.Scopes);

        var response = await SendAsync(row, clientId, Rsin);

        Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await NotificatiesRequests.DescribeAsync(response));
    }

    [Theory]
    [MemberData(nameof(AcceptedScopes))]
    public async Task Client_with_only_one_of_the_actions_scopes_gets_past_the_scope_filter(string row, string scope)
    {
        var response = await SendAsync(row, NrcClients.Authorized(_factory, scope), Rsin);

        Assert.True(
            response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && (int)response.StatusCode < 500,
            await NotificatiesRequests.DescribeAsync(response)
        );
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task Client_with_all_authorizations_gets_past_the_scope_filter(string row)
    {
        var response = await SendAsync(row, NrcClients.WithAllAuthorizations(_factory), Rsin);

        Assert.True(
            response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) && (int)response.StatusCode < 500,
            await NotificatiesRequests.DescribeAsync(response)
        );
    }

    [Fact]
    public async Task Forbidden_writes_change_nothing()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var abonnement = await NotificatiesSeed.AbonnementAsync(_factory, Rsin, kanaal);
        var consumeOnly = NrcClients.Authorized(_factory, OneGround.ZGW.Common.Web.Authorization.AuthorizationScopes.Notificaties.Consume);
        var produceOnly = NrcClients.Authorized(_factory, OneGround.ZGW.Common.Web.Authorization.AuthorizationScopes.Notificaties.Produce);
        var newKanaalNaam = NotificatiesSeed.NewKanaalNaam();
        var newAuth = NotificatiesSeed.NewAuth();
        var changedAuth = NotificatiesSeed.NewAuth();

        // consumeren is needed to write abonnementen; publiceren only reads them
        var responses = new[]
        {
            await _client.SendAsync(NotificatiesRequests.CreateAbonnement(kanaal.Naam, newAuth, produceOnly, Rsin)),
            await _client.SendAsync(NotificatiesRequests.UpdateAbonnement(abonnement.Id, kanaal.Naam, changedAuth, produceOnly, Rsin)),
            await _client.SendAsync(NotificatiesRequests.PartialUpdateAbonnement(abonnement.Id, changedAuth, produceOnly, Rsin)),
            await _client.SendAsync(NotificatiesRequests.DeleteAbonnement(abonnement.Id, produceOnly, Rsin)),
            // publiceren is needed to write kanalen; consumeren only reads them
            await _client.SendAsync(NotificatiesRequests.CreateKanaal(newKanaalNaam, consumeOnly, Rsin)),
        };

        foreach (var response in responses)
        {
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden, await NotificatiesRequests.DescribeAsync(response));
        }

        var stored = await NotificatiesSeed.FindAbonnementAsync(_factory, abonnement.Id);
        Assert.NotNull(stored);
        Assert.Equal(abonnement.Auth, stored.Auth);
        Assert.Empty(await NotificatiesSeed.FindAbonnementenByAuthAsync(_factory, newAuth));
        Assert.Empty(await NotificatiesSeed.FindAbonnementenByAuthAsync(_factory, changedAuth));
        Assert.Null(await NotificatiesSeed.FindKanaalByNaamAsync(_factory, newKanaalNaam));
    }

    private async Task<HttpResponseMessage> SendAsync(string row, string clientId, string rsin)
    {
        var matrixRow = ScopeMatrix.Named(row);
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var resourceId = matrixRow.Needs switch
        {
            NrcResource.Kanaal => kanaal.Id,
            NrcResource.Abonnement => (await NotificatiesSeed.AbonnementAsync(_factory, Rsin, kanaal)).Id,
            _ => Guid.NewGuid(),
        };

        return await _client.SendAsync(NotificatiesRequests.For(matrixRow, resourceId, kanaal.Naam, clientId, rsin));
    }
}
