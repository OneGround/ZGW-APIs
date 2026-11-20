using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Abonnementen belong to the organisation (RSIN) of the token that created them; kanalen are shared by every organisation.
/// </summary>
[Collection(NotificatiesApiCollection.Name)]
public class OrganisationSeparationTests
{
    private const string OwnRsin = TestRsins.B;
    private const string OtherRsin = TestRsins.C;

    private readonly NotificatiesWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganisationSeparationTests(NotificatiesApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task List_contains_only_abonnementen_owned_by_the_organisation_of_the_request()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var own = await NotificatiesSeed.AbonnementAsync(_factory, OwnRsin, kanaal);
        var other = await NotificatiesSeed.AbonnementAsync(_factory, OtherRsin, kanaal);
        var clientId = NrcClients.WithAllAuthorizations(_factory);

        var ownList = await ListIdsAsync(clientId, OwnRsin);
        var otherList = await ListIdsAsync(clientId, OtherRsin);

        Assert.Contains(own.Id, ownList);
        Assert.DoesNotContain(other.Id, ownList);
        Assert.Contains(other.Id, otherList);
        Assert.DoesNotContain(own.Id, otherList);
    }

    [Fact]
    public async Task Abonnement_of_another_organisation_is_not_found()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var own = await NotificatiesSeed.AbonnementAsync(_factory, OwnRsin, kanaal);
        var other = await NotificatiesSeed.AbonnementAsync(_factory, OtherRsin, kanaal);
        var clientId = NrcClients.WithAllAuthorizations(_factory);

        var ownResponse = await _client.SendAsync(NotificatiesRequests.GetAbonnement(own.Id, clientId, OwnRsin));
        var otherResponse = await _client.SendAsync(NotificatiesRequests.GetAbonnement(other.Id, clientId, OwnRsin));

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.True(otherResponse.StatusCode == HttpStatusCode.NotFound, await NotificatiesRequests.DescribeAsync(otherResponse));
    }

    [Fact]
    public async Task Update_and_delete_of_an_abonnement_of_another_organisation_are_not_found_and_change_nothing()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var other = await NotificatiesSeed.AbonnementAsync(_factory, OtherRsin, kanaal);
        var clientId = NrcClients.Authorized(_factory, AuthorizationScopes.Notificaties.Consume);
        var newAuth = NotificatiesSeed.NewAuth();

        var responses = new[]
        {
            await _client.SendAsync(NotificatiesRequests.UpdateAbonnement(other.Id, kanaal.Naam, newAuth, clientId, OwnRsin)),
            await _client.SendAsync(NotificatiesRequests.PartialUpdateAbonnement(other.Id, newAuth, clientId, OwnRsin)),
            await _client.SendAsync(NotificatiesRequests.DeleteAbonnement(other.Id, clientId, OwnRsin)),
        };

        foreach (var response in responses)
        {
            Assert.True(response.StatusCode == HttpStatusCode.NotFound, await NotificatiesRequests.DescribeAsync(response));
        }

        var stored = await NotificatiesSeed.FindAbonnementAsync(_factory, other.Id);
        Assert.NotNull(stored);
        Assert.Equal(other.Auth, stored.Auth);
        Assert.Equal(OtherRsin, stored.Owner);
        Assert.Empty(await NotificatiesSeed.FindAbonnementenByAuthAsync(_factory, newAuth));
    }

    [Fact]
    public async Task Created_abonnement_is_owned_by_the_organisation_of_the_token_not_of_the_client()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var clientId = NrcClients.Authorized(_factory, AuthorizationScopes.Notificaties.Consume);
        var auth = NotificatiesSeed.NewAuth();

        var response = await _client.SendAsync(NotificatiesRequests.CreateAbonnement(kanaal.Naam, auth, clientId, OwnRsin));

        Assert.True(response.StatusCode == HttpStatusCode.Created, await NotificatiesRequests.DescribeAsync(response));
        var stored = Assert.Single(await NotificatiesSeed.FindAbonnementenByAuthAsync(_factory, auth));
        Assert.Equal(OwnRsin, stored.Owner);

        var seenByOther = await ListIdsAsync(clientId, OtherRsin);
        Assert.DoesNotContain(stored.Id, seenByOther);
    }

    [Fact]
    public async Task Update_and_delete_of_an_abonnement_of_the_own_organisation_succeed()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var own = await NotificatiesSeed.AbonnementAsync(_factory, OwnRsin, kanaal);
        var clientId = NrcClients.Authorized(_factory, AuthorizationScopes.Notificaties.Consume);
        var newAuth = NotificatiesSeed.NewAuth();

        var update = await _client.SendAsync(NotificatiesRequests.PartialUpdateAbonnement(own.Id, newAuth, clientId, OwnRsin));
        Assert.True(update.StatusCode == HttpStatusCode.OK, await NotificatiesRequests.DescribeAsync(update));
        Assert.Equal(newAuth, (await NotificatiesSeed.FindAbonnementAsync(_factory, own.Id)).Auth);

        var delete = await _client.SendAsync(NotificatiesRequests.DeleteAbonnement(own.Id, clientId, OwnRsin));
        Assert.True(delete.StatusCode == HttpStatusCode.NoContent, await NotificatiesRequests.DescribeAsync(delete));
        Assert.Null(await NotificatiesSeed.FindAbonnementAsync(_factory, own.Id));
    }

    [Fact]
    public async Task Kanaal_is_shared_by_every_organisation()
    {
        var clientId = NrcClients.Authorized(_factory, AuthorizationScopes.Notificaties.Produce);
        var naam = NotificatiesSeed.NewKanaalNaam();

        var create = await _client.SendAsync(NotificatiesRequests.CreateKanaal(naam, clientId, OwnRsin));
        Assert.True(create.StatusCode == HttpStatusCode.Created, await NotificatiesRequests.DescribeAsync(create));
        var kanaal = await NotificatiesSeed.FindKanaalByNaamAsync(_factory, naam);

        var read = await _client.SendAsync(NotificatiesRequests.GetKanaal(kanaal.Id, clientId, OtherRsin));
        var list = await _client.SendAsync(NotificatiesRequests.GetAllKanalen(clientId, OtherRsin));

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(naam, ReadStrings(await list.Content.ReadAsStringAsync(), "naam"));
    }

    [Fact]
    public async Task Queued_notification_carries_the_organisation_of_the_token()
    {
        var kanaal = await NotificatiesSeed.KanaalAsync(_factory, "bron");
        var clientId = NrcClients.Authorized(_factory, AuthorizationScopes.Notificaties.Produce);

        var fromOwn = await _client.SendAsync(NotificatiesRequests.Notify(kanaal.Naam, clientId, OwnRsin));
        var fromOther = await _client.SendAsync(NotificatiesRequests.Notify(kanaal.Naam, clientId, OtherRsin));

        Assert.True(fromOwn.StatusCode == HttpStatusCode.OK, await NotificatiesRequests.DescribeAsync(fromOwn));
        Assert.True(fromOther.StatusCode == HttpStatusCode.OK, await NotificatiesRequests.DescribeAsync(fromOther));
        var queued = _factory.QueuedNotifications.Where(n => n.Kanaal == kanaal.Naam).Select(n => n.Rsin).ToList();
        Assert.Equal(new[] { OwnRsin, OtherRsin }.Order(), queued.Order());
    }

    private async Task<List<Guid>> ListIdsAsync(string clientId, string rsin)
    {
        var response = await _client.SendAsync(NotificatiesRequests.GetAllAbonnementen(clientId, rsin));
        Assert.True(response.StatusCode == HttpStatusCode.OK, await NotificatiesRequests.DescribeAsync(response));

        // The API masks the auth of an abonnement, so abonnementen are told apart by the id in their url
        return ReadStrings(await response.Content.ReadAsStringAsync(), "url").Select(u => Guid.Parse(u[(u.LastIndexOf('/') + 1)..])).ToList();
    }

    private static List<string> ReadStrings(string json, string property)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.EnumerateArray().Select(e => e.GetProperty(property).GetString()).ToList();
    }
}
