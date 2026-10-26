using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Contracts.v1._7;
using OneGround.ZGW.Zaken.Web.Controllers;
using Xunit;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// The ZAAKNOTITIE resource (v1.7), run against the real API and a real PostgreSQL database: the filters (including the case-insensitive 'contains'
/// ones, which the in-memory provider cannot run), the paging, the status rule and the authorization of the zaak a notitie belongs to.
/// </summary>
[Collection(ZakenApiCollection.Name)]
public class ZaakNotitiesTests
{
    private const string Rsin = TestRsins.A;
    private const string OtherRsin = TestRsins.B;

    private readonly ZakenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ZaakNotitiesTests(ZakenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    // ---- create and read

    [Fact]
    public async Task Create_gives_a_concept_intern_notitie_when_type_and_status_are_not_given()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();

        var response = await SendAsync(HttpMethod.Post, "/" + ApiRoutes.ZaakNotities.Create, clientId, Body(zaakUrl, "Onderwerp", "Tekst"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var notitie = await ReadAsync(response);
        Assert.EndsWith(notitie.Value<string>("url"), response.Headers.Location!.ToString());
        Assert.Contains("/zaaknotities/", notitie.Value<string>("url"));
        Assert.Equal("concept", notitie.Value<string>("status"));
        Assert.Equal("intern", notitie.Value<string>("notitieType"));
        Assert.Equal(zaakUrl, notitie.Value<string>("gerelateerdAan"));
        Assert.NotNull(notitie["aanmaakdatum"]);
        Assert.NotNull(notitie["wijzigingsdatum"]);

        var read = await SendAsync(HttpMethod.Get, new Uri(notitie.Value<string>("url")!).AbsolutePath, clientId);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(notitie.Value<string>("url"), (await ReadAsync(read)).Value<string>("url"));
    }

    [Fact]
    public async Task Create_without_the_required_fields_is_a_bad_request()
    {
        var response = await SendAsync(HttpMethod.Post, "/" + ApiRoutes.ZaakNotities.Create, NewClientWithAllAuthorizations(), new JObject());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var names = (await ReadAsync(response))["invalidParams"]!.Select(p => p.Value<string>("name")).ToArray();
        Assert.Contains("onderwerp", names);
        Assert.Contains("tekst", names);
        Assert.Contains("gerelateerdAan", names);
    }

    [Fact]
    public async Task Create_with_an_unknown_property_is_a_bad_request()
    {
        var body = Body(await SeedZaakUrlAsync(), "Onderwerp", "Tekst");
        body["onbekend"] = "x";

        var response = await SendAsync(HttpMethod.Post, "/" + ApiRoutes.ZaakNotities.Create, NewClientWithAllAuthorizations(), body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_for_an_unknown_zaak_is_a_bad_request_on_gerelateerdAan()
    {
        var unknownZaakUrl = ZaakUrl(Guid.NewGuid());

        var response = await SendAsync(
            HttpMethod.Post,
            "/" + ApiRoutes.ZaakNotities.Create,
            NewClientWithAllAuthorizations(),
            Body(unknownZaakUrl, "O", "T")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("gerelateerdAan", (await ReadAsync(response))["invalidParams"]![0]!.Value<string>("name"));
    }

    [Fact]
    public async Task Create_for_a_zaak_of_a_zaaktype_the_client_may_not_update_is_forbidden()
    {
        var zaakUrl = await SeedZaakUrlAsync(ZaakTypes.A);

        var response = await SendAsync(
            HttpMethod.Post,
            "/" + ApiRoutes.ZaakNotities.Create,
            NewClientAuthorized(ZaakTypes.B, AuthorizationScopes.Zaken.Update),
            Body(zaakUrl, "O", "T")
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- update

    [Fact]
    public async Task Put_replaces_the_notitie_and_patch_changes_only_the_given_fields()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();
        var notitie = await CreateAsync(clientId, zaakUrl, onderwerp: "Oud", tekst: "Oude tekst", aangemaaktDoor: "auteur");
        var path = new Uri(notitie.Value<string>("url")!).AbsolutePath;

        var put = await SendAsync(HttpMethod.Put, path, clientId, Body(zaakUrl, "Nieuw", "Nieuwe tekst"));

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var replaced = await ReadAsync(put);
        Assert.Equal("Nieuw", replaced.Value<string>("onderwerp"));
        Assert.Equal("Nieuwe tekst", replaced.Value<string>("tekst"));
        Assert.Null(replaced.Value<string>("aangemaaktDoor"));

        var patch = await SendAsync(HttpMethod.Patch, path, clientId, new JObject { ["tekst"] = "Alleen de tekst" });

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var patched = await ReadAsync(patch);
        Assert.Equal("Nieuw", patched.Value<string>("onderwerp"));
        Assert.Equal("Alleen de tekst", patched.Value<string>("tekst"));
        Assert.Equal(zaakUrl, patched.Value<string>("gerelateerdAan"));
    }

    [Fact]
    public async Task Patch_is_allowed_for_a_client_that_may_update_but_not_read()
    {
        var zaakUrl = await SeedZaakUrlAsync();
        var notitie = await CreateAsync(NewClientWithAllAuthorizations(), zaakUrl);
        var updateOnly = NewClientAuthorized(ZaakTypes.A, AuthorizationScopes.Zaken.Update);
        var path = new Uri(notitie.Value<string>("url")!).AbsolutePath;

        var patch = await SendAsync(HttpMethod.Patch, path, updateOnly, new JObject { ["tekst"] = "Alleen de tekst" });
        var read = await SendAsync(HttpMethod.Get, path, updateOnly);

        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }

    [Fact]
    public async Task Put_without_type_and_status_keeps_the_stored_values()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();
        var notitie = await CreateAsync(clientId, zaakUrl, notitieType: "extern");

        var put = await SendAsync(
            HttpMethod.Put,
            new Uri(notitie.Value<string>("url")!).AbsolutePath,
            clientId,
            Body(zaakUrl, "Nieuw", "Nieuwe tekst")
        );

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var replaced = await ReadAsync(put);
        Assert.Equal("extern", replaced.Value<string>("notitieType"));
        Assert.Equal("concept", replaced.Value<string>("status"));
    }

    [Fact]
    public async Task Modifying_a_notitie_of_a_closed_zaak_without_forced_update_is_forbidden_with_an_explanation()
    {
        var zaakUrl = await SeedZaakUrlAsync(closed: true);
        var withoutForcedUpdate = NewClientAuthorized(ZaakTypes.A, AuthorizationScopes.Zaken.Update);

        var response = await SendAsync(HttpMethod.Post, "/" + ApiRoutes.ZaakNotities.Create, withoutForcedUpdate, Body(zaakUrl, "O", "T"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("zaak", (await ReadAsync(response))["invalidParams"]![0]!.Value<string>("name"));
    }

    [Fact]
    public async Task A_definitief_notitie_cannot_be_changed_but_can_be_deleted()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();
        var notitie = await CreateAsync(clientId, zaakUrl);
        var path = new Uri(notitie.Value<string>("url")!).AbsolutePath;

        var finalize = await SendAsync(HttpMethod.Patch, path, clientId, new JObject { ["status"] = "definitief" });
        var put = await SendAsync(HttpMethod.Put, path, clientId, Body(zaakUrl, "Nieuw", "Nieuwe tekst"));
        var reopen = await SendAsync(HttpMethod.Patch, path, clientId, new JObject { ["status"] = "concept" });
        var delete = await SendAsync(HttpMethod.Delete, path, clientId);

        Assert.Equal(HttpStatusCode.OK, finalize.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        Assert.Equal("status", (await ReadAsync(put))["invalidParams"]![0]!.Value<string>("name"));
        Assert.Equal(HttpStatusCode.BadRequest, reopen.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task A_notitie_cannot_be_moved_to_another_zaak()
    {
        var clientId = NewClientWithAllAuthorizations();
        var notitie = await CreateAsync(clientId, await SeedZaakUrlAsync());
        var otherZaakUrl = await SeedZaakUrlAsync();

        var response = await SendAsync(
            HttpMethod.Put,
            new Uri(notitie.Value<string>("url")!).AbsolutePath,
            clientId,
            Body(otherZaakUrl, "Onderwerp", "Tekst")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("gerelateerdAan", (await ReadAsync(response))["invalidParams"]![0]!.Value<string>("name"));
    }

    [Fact]
    public async Task Delete_removes_the_notitie()
    {
        var clientId = NewClientWithAllAuthorizations();
        var notitie = await CreateAsync(clientId, await SeedZaakUrlAsync());
        var path = new Uri(notitie.Value<string>("url")!).AbsolutePath;

        var delete = await SendAsync(HttpMethod.Delete, path, clientId);
        var read = await SendAsync(HttpMethod.Get, path, clientId);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [Fact]
    public async Task A_notitie_of_another_organisation_is_not_found()
    {
        var clientId = NewClientWithAllAuthorizations();
        var notitie = await CreateAsync(clientId, await SeedZaakUrlAsync(rsin: Rsin));

        var response = await SendAsync(HttpMethod.Get, new Uri(notitie.Value<string>("url")!).AbsolutePath, clientId, rsin: OtherRsin);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- list

    [Fact]
    public async Task List_filters_on_text_fields_type_and_status()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();
        await CreateAsync(clientId, zaakUrl, onderwerp: "Korting 100% akkoord", tekst: "Eerste", aangemaaktDoor: "Jan Jansen");
        await CreateAsync(clientId, zaakUrl, onderwerp: "Korting_100 akkoord", tekst: "Tweede", aangemaaktDoor: "Piet", notitieType: "extern");
        await CreateAsync(clientId, zaakUrl, onderwerp: "Overig", tekst: "Derde", aangemaaktDoor: "Jan", status: "definitief");

        Assert.Equal(["Eerste"], await ListTekstenAsync(clientId, zaakUrl, "onderwerp__icontains=100%25"));
        Assert.Equal(["Tweede"], await ListTekstenAsync(clientId, zaakUrl, "onderwerp__icontains=_100"));
        Assert.Equal(["Eerste", "Tweede"], await ListTekstenAsync(clientId, zaakUrl, "onderwerp__icontains=KORTING"));
        Assert.Equal(["Eerste"], await ListTekstenAsync(clientId, zaakUrl, "aangemaaktDoor__icontains=JANSEN"));
        Assert.Equal(["Derde"], await ListTekstenAsync(clientId, zaakUrl, "aangemaaktDoor=Jan"));
        Assert.Equal(["Tweede"], await ListTekstenAsync(clientId, zaakUrl, "notitieType=extern"));
        Assert.Equal(["Derde"], await ListTekstenAsync(clientId, zaakUrl, "status=definitief"));
        Assert.Equal(["Derde"], await ListTekstenAsync(clientId, zaakUrl, "tekst__icontains=DERD"));
        Assert.Equal(["Tweede"], await ListTekstenAsync(clientId, zaakUrl, "tekst=Tweede"));
    }

    [Fact]
    public async Task List_filters_on_the_dates()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();
        await CreateAsync(clientId, zaakUrl, tekst: "Eén");

        var yesterday = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-1).ToString("O"));
        var tomorrow = Uri.EscapeDataString(DateTime.UtcNow.AddDays(1).ToString("O"));

        Assert.Equal(["Eén"], await ListTekstenAsync(clientId, zaakUrl, $"aanmaakdatum__gte={yesterday}", $"aanmaakdatum__lt={tomorrow}"));
        Assert.Equal(["Eén"], await ListTekstenAsync(clientId, zaakUrl, $"wijzigingsdatum__gt={yesterday}"));
        Assert.Empty(await ListTekstenAsync(clientId, zaakUrl, $"aanmaakdatum__gt={tomorrow}"));
        Assert.Empty(await ListTekstenAsync(clientId, zaakUrl, $"wijzigingsdatum__lte={yesterday}"));
    }

    [Fact]
    public async Task List_is_paged_with_the_given_pageSize()
    {
        var clientId = NewClientWithAllAuthorizations();
        var zaakUrl = await SeedZaakUrlAsync();
        foreach (var tekst in new[] { "1", "2", "3" })
        {
            await CreateAsync(clientId, zaakUrl, tekst: tekst);
        }

        var first = await ListAsync(clientId, zaakUrl, "pageSize=2");
        var second = await ListAsync(clientId, zaakUrl, "pageSize=2", "page=2");

        Assert.Equal(3, first.Value<int>("count"));
        Assert.Equal(["1", "2"], first["results"]!.Select(r => r.Value<string>("tekst")));
        Assert.Contains("pageSize=2", first.Value<string>("next"));
        Assert.Null(first.Value<string>("previous"));
        Assert.Equal(["3"], second["results"]!.Select(r => r.Value<string>("tekst")));
        Assert.Null(second.Value<string>("next"));
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=abc")]
    [InlineData("notitieType=onbekend")]
    [InlineData("status=onbekend")]
    [InlineData("aanmaakdatum=geen-datum")]
    [InlineData("gerelateerdAan=geen-url")]
    [InlineData("onbekend=1")]
    public async Task List_with_an_invalid_parameter_is_a_bad_request(string query)
    {
        var response = await SendAsync(HttpMethod.Get, "/" + ApiRoutes.ZaakNotities.GetAll + "?" + query, NewClientWithAllAuthorizations());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_only_has_the_notities_of_zaken_the_client_is_authorized_for()
    {
        var zaakUrl = await SeedZaakUrlAsync(ZaakTypes.A);
        await CreateAsync(NewClientWithAllAuthorizations(), zaakUrl, tekst: "Zichtbaar");

        var forA = await ListTekstenAsync(NewClientAuthorized(ZaakTypes.A, AuthorizationScopes.Zaken.Read), zaakUrl);
        var forB = await ListTekstenAsync(NewClientAuthorized(ZaakTypes.B, AuthorizationScopes.Zaken.Read), zaakUrl);

        Assert.Equal(["Zichtbaar"], forA);
        Assert.Empty(forB);
    }

    [Fact]
    public async Task A_notitie_of_a_zaak_the_client_may_not_read_is_forbidden()
    {
        var notitie = await CreateAsync(NewClientWithAllAuthorizations(), await SeedZaakUrlAsync(ZaakTypes.A));

        var response = await SendAsync(
            HttpMethod.Get,
            new Uri(notitie.Value<string>("url")!).AbsolutePath,
            NewClientAuthorized(ZaakTypes.B, AuthorizationScopes.Zaken.Read)
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- helpers

    private async Task<JObject> CreateAsync(
        string clientId,
        string zaakUrl,
        string onderwerp = "Onderwerp",
        string tekst = "Tekst",
        string aangemaaktDoor = null,
        string notitieType = null,
        string status = null
    )
    {
        var body = Body(zaakUrl, onderwerp, tekst);
        body["aangemaaktDoor"] = aangemaaktDoor;
        body["notitieType"] = notitieType;
        body["status"] = status;

        var response = await SendAsync(HttpMethod.Post, "/" + ApiRoutes.ZaakNotities.Create, clientId, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadAsync(response);
    }

    private async Task<JObject> ListAsync(string clientId, string zaakUrl, params string[] query)
    {
        var parameters = new[] { $"gerelateerdAan={Uri.EscapeDataString(zaakUrl)}" }.Concat(query);
        var response = await SendAsync(HttpMethod.Get, "/" + ApiRoutes.ZaakNotities.GetAll + "?" + string.Join("&", parameters), clientId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await ReadAsync(response);
    }

    private async Task<string[]> ListTekstenAsync(string clientId, string zaakUrl, params string[] query)
    {
        var list = await ListAsync(clientId, zaakUrl, query);

        return list["results"]!.Select(r => r.Value<string>("tekst")).Order().ToArray();
    }

    private static JObject Body(string zaakUrl, string onderwerp, string tekst) =>
        new()
        {
            ["gerelateerdAan"] = zaakUrl,
            ["onderwerp"] = onderwerp,
            ["tekst"] = tekst,
        };

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string clientId, JObject body = null, string rsin = Rsin)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_7);
        request.Headers.Authorization = TestIdentity.Create(clientId, rsin);

        if (body != null)
        {
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        }

        return await _client.SendAsync(request);
    }

    private static async Task<JObject> ReadAsync(HttpResponseMessage response) => JObject.Parse(await response.Content.ReadAsStringAsync());

    private string ZaakUrl(Guid zaakId) => new Uri(_client.BaseAddress!, $"api/v1/zaken/{zaakId}").ToString();

    private string NewClientWithAllAuthorizations()
    {
        var clientId = NewClientId();
        _factory.AutorisatiesApi.GrantAllAuthorizations(clientId);

        return clientId;
    }

    private string NewClientAuthorized(string zaakType, string scope)
    {
        var clientId = NewClientId();

        _factory.AutorisatiesApi.Grant(
            clientId,
            new AutorisatieResponseDto
            {
                Component = "zrc",
                Scopes = [scope],
                ZaakType = zaakType,
                MaxVertrouwelijkheidaanduiding = nameof(VertrouwelijkheidAanduiding.zeer_geheim),
            }
        );

        return clientId;
    }

    private async Task<string> SeedZaakUrlAsync(string zaakType = ZaakTypes.A, string rsin = Rsin, bool closed = false)
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
            VertrouwelijkheidAanduiding = VertrouwelijkheidAanduiding.openbaar,
            Einddatum = closed ? DateOnly.FromDateTime(DateTime.UtcNow) : null,
        };

        context.Zaken.Add(zaak);
        await context.SaveChangesAsync();

        return ZaakUrl(zaak.Id);
    }

    private static string NewClientId() => $"integration-test-{Guid.NewGuid():N}";
}
