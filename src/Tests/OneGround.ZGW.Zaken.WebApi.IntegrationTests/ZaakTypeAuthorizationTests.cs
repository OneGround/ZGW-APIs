using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Zaken.DataModel;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// GET /zaken only returns zaken of the zaaktypes the client is authorized for. This only passes when the API's own
/// query handler filters on the authorization context the scope filter built.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakTypeAuthorizationTests
{
    // An rsin of its own, so zaken seeded by other tests cannot show up here
    private const string Rsin = "444555666";

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakTypeAuthorizationTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Zaken_are_only_listed_for_the_zaaktypes_the_client_is_authorized_for()
    {
        var zaakId = await SeedZaakAsync(ZaakTypes.A);

        var clientForB = NewClientAuthorizedToReadZaakType(ZaakTypes.B);
        var clientForA = NewClientAuthorizedToReadZaakType(ZaakTypes.A);

        var responseForB = await _client.SendAsync(ZakenRequests.GetAllZaken(clientForB, Rsin));
        var responseForA = await _client.SendAsync(ZakenRequests.GetAllZaken(clientForA, Rsin));

        Assert.Equal(HttpStatusCode.OK, responseForB.StatusCode);
        var zakenForB = await ReadResultsAsync(responseForB);
        Assert.Empty(zakenForB);

        Assert.Equal(HttpStatusCode.OK, responseForA.StatusCode);
        var zakenForA = await ReadResultsAsync(responseForA);
        var zaak = Assert.Single(zakenForA);
        Assert.EndsWith($"/zaken/{zaakId}", zaak.Value<string>("url"));
        Assert.Equal(ZaakTypes.A, zaak.Value<string>("zaaktype"));
    }

    private string NewClientAuthorizedToReadZaakType(string zaakType)
    {
        var clientId = $"integration-test-{Guid.NewGuid():N}";

        _factory.AuthorizationResolver.GrantPermissions(
            clientId,
            new AuthorizationPermission
            {
                ZaakType = zaakType,
                Scopes = [AuthorizationScopes.Zaken.Read],
                MaximumVertrouwelijkheidAanduiding = (int)VertrouwelijkheidAanduiding.zeer_geheim,
            }
        );

        return clientId;
    }

    private async Task<Guid> SeedZaakAsync(string zaakType)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        var zaak = new Zaak
        {
            Id = Guid.NewGuid(),
            Owner = Rsin,
            Bronorganisatie = Rsin,
            VerantwoordelijkeOrganisatie = Rsin,
            Identificatie = $"ZAAK-{Guid.NewGuid():N}",
            Zaaktype = zaakType,
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            Communicatiekanaal = "",
            Selectielijstklasse = "",
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
        };

        context.Zaken.Add(zaak);
        await context.SaveChangesAsync();

        return zaak.Id;
    }

    private static async Task<JToken[]> ReadResultsAsync(HttpResponseMessage response)
    {
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());

        return body["results"]!.ToArray();
    }
}
