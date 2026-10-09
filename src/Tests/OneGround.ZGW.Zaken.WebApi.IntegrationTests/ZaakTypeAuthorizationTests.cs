using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.Zaken.DataModel;
using Xunit;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Which zaken a client gets, decided by the API's query handlers from the authorizations its resolver returned.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakTypeAuthorizationTests
{
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
        const string rsin = TestRsins.B;
        var zaakId = await SeedZaakAsync(rsin, ZaakTypes.A);

        var zakenForB = await GetAllZakenAsync(NewClientAuthorizedToRead(ZaakTypes.B), rsin);
        var zakenForA = await GetAllZakenAsync(NewClientAuthorizedToRead(ZaakTypes.A), rsin);

        Assert.Empty(zakenForB);
        var zaak = Assert.Single(zakenForA);
        Assert.EndsWith($"/zaken/{zaakId}", zaak.Value<string>("url"));
        Assert.Equal(ZaakTypes.A, zaak.Value<string>("zaaktype"));
    }

    [Fact]
    public async Task Zaken_above_the_clients_maximum_vertrouwelijkheidaanduiding_are_not_listed()
    {
        const string rsin = TestRsins.C;
        await SeedZaakAsync(rsin, ZaakTypes.A, VertrouwelijkheidAanduiding.geheim);

        var belowMaximum = await GetAllZakenAsync(NewClientAuthorizedToRead(ZaakTypes.A, VertrouwelijkheidAanduiding.vertrouwelijk), rsin);
        var atMaximum = await GetAllZakenAsync(NewClientAuthorizedToRead(ZaakTypes.A, VertrouwelijkheidAanduiding.geheim), rsin);

        Assert.Empty(belowMaximum);
        Assert.Single(atMaximum);
    }

    [Fact]
    public async Task Client_with_all_authorizations_gets_zaken_of_every_zaaktype()
    {
        const string rsin = TestRsins.D;
        await SeedZaakAsync(rsin, ZaakTypes.A, VertrouwelijkheidAanduiding.zeer_geheim);
        await SeedZaakAsync(rsin, ZaakTypes.B, VertrouwelijkheidAanduiding.zeer_geheim);

        var zaken = await GetAllZakenAsync(NewClientWithAllAuthorizations(), rsin);

        Assert.Equal([ZaakTypes.A, ZaakTypes.B], zaken.Select(z => z.Value<string>("zaaktype")).Order());
    }

    [Fact]
    public async Task Zaken_of_another_organisation_are_not_listed()
    {
        await SeedZaakAsync(TestRsins.E, ZaakTypes.A);

        var zaken = await GetAllZakenAsync(NewClientWithAllAuthorizations(), TestRsins.F);

        Assert.Empty(zaken);
    }

    [Fact]
    public async Task Zaak_of_a_zaaktype_the_client_is_not_authorized_for_is_forbidden()
    {
        const string rsin = TestRsins.G;
        var zaakId = await SeedZaakAsync(rsin, ZaakTypes.A);

        var responseForB = await _client.SendAsync(ZakenRequests.GetZaak(zaakId, NewClientAuthorizedToRead(ZaakTypes.B), rsin));
        var responseForA = await _client.SendAsync(ZakenRequests.GetZaak(zaakId, NewClientAuthorizedToRead(ZaakTypes.A), rsin));

        Assert.Equal(HttpStatusCode.Forbidden, responseForB.StatusCode);
        Assert.Equal(HttpStatusCode.OK, responseForA.StatusCode);
    }

    private string NewClientAuthorizedToRead(string zaakType, VertrouwelijkheidAanduiding maximum = VertrouwelijkheidAanduiding.zeer_geheim)
    {
        var clientId = NewClientId();

        _factory.AutorisatiesApi.Grant(
            clientId,
            new AutorisatieResponseDto
            {
                Component = "zrc",
                Scopes = [AuthorizationScopes.Zaken.Read],
                ZaakType = zaakType,
                MaxVertrouwelijkheidaanduiding = maximum.ToString(),
            }
        );

        return clientId;
    }

    private string NewClientWithAllAuthorizations()
    {
        var clientId = NewClientId();
        _factory.AutorisatiesApi.GrantAllAuthorizations(clientId);

        return clientId;
    }

    private async Task<JToken[]> GetAllZakenAsync(string clientId, string rsin)
    {
        var response = await _client.SendAsync(ZakenRequests.GetAllZaken(clientId, rsin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JObject.Parse(await response.Content.ReadAsStringAsync());

        return body["results"]!.ToArray();
    }

    private async Task<Guid> SeedZaakAsync(
        string rsin,
        string zaakType,
        VertrouwelijkheidAanduiding vertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar
    )
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ZrcDbContext>();

        var zaak = new Zaak
        {
            Id = Guid.NewGuid(),
            Owner = rsin,
            Bronorganisatie = rsin,
            VerantwoordelijkeOrganisatie = rsin,
            Identificatie = $"ZAAK-{Guid.NewGuid():N}",
            Zaaktype = zaakType,
            Startdatum = DateOnly.FromDateTime(DateTime.UtcNow),
            Communicatiekanaal = "",
            Selectielijstklasse = "",
            VertrouwelijkheidAanduiding = vertrouwelijkheidAanduiding,
        };

        context.Zaken.Add(zaak);
        await context.SaveChangesAsync();

        return zaak.Id;
    }

    private static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
