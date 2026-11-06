using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Gesprekken.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// The MijnGesprekken flow against the real API and a real PostgreSQL database: start a gesprek, add bijdragen,
/// register reading and end the gesprek.
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class GesprekkenFlowTests
{
    private const string Rsin = TestRsins.A;
    private const string Partij = FlowBody.Partij;
    private const string Actor = "https://klantinteracties.integrationtest.invalid/api/v1/actoren/medewerker";

    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GesprekkenFlowTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task A_gesprek_goes_from_start_to_end()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();

        // Start: gesprek, ontvanger, verzender and first bijdrage in one request
        var created = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var gesprek = await ReadAsync(created);
        var gesprekUrl = gesprek.Value<string>("url");
        var gesprekId = gesprek.Value<string>("uuid");
        Assert.StartsWith("G", gesprek.Value<string>("referentie"));
        Assert.Null(gesprek.Value<string>("momentEindeGesprek"));

        // Listed under its zaak
        var list = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?aanleidingZaak={Uri.EscapeDataString(zaak)}", clientId));
        Assert.Equal(1, list.Value<int>("count"));

        // Detail contains the first bijdrage; find the deelname of the burger through the list filter on partij
        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprekId}", clientId));
        var eerste = detail["gespreksbijdragen"]!.Single();
        var medewerkerDeelname = eerste.Value<string>("verzender");

        // Deelnames are embedded, with a reference to the partij or actor
        var deelnames = (JArray)detail["gespreksdeelnames"]!;
        Assert.Equal(["partij", "actor"], deelnames.Select(d => d.Value<string>("type")).OrderByDescending(t => t).ToArray());
        Assert.Equal(Partij, deelnames.Single(d => d.Value<string>("type") == "partij").Value<string>("partij"));

        // A reply from a new deelname of the burger
        var reply = await SendAsync(
            HttpMethod.Post,
            "/api/v1/gespreksbijdragen",
            clientId,
            new JObject
            {
                ["gesprek"] = gesprekUrl,
                ["tekstGespreksBijdrage"] = "Hierbij mijn antwoord.",
                ["isReactieOp"] = eerste.Value<string>("url"),
                ["verzender"] = new JObject { ["nieuweDeelname"] = FlowBody.Deelname("partij") },
            }
        );
        Assert.Equal(HttpStatusCode.Created, reply.StatusCode);
        var bijdrage = await ReadAsync(reply);
        Assert.Equal(gesprekUrl, bijdrage.Value<string>("gesprek"));
        Assert.Equal(eerste.Value<string>("url"), bijdrage.Value<string>("isReactieOp"));
        Assert.Contains("/gespreksdeelnames/", bijdrage.Value<string>("verzender"));

        // Read twice: same registration
        var gelezen1 = await ReadAsync(
            await SendAsync(
                HttpMethod.Post,
                $"/api/v1/gespreksbijdragen/{bijdrage.Value<string>("uuid")}/gelezen",
                clientId,
                new JObject { ["gespreksdeelname"] = medewerkerDeelname }
            )
        );
        var gelezen2 = await ReadAsync(
            await SendAsync(
                HttpMethod.Post,
                $"/api/v1/gespreksbijdragen/{bijdrage.Value<string>("uuid")}/gelezen",
                clientId,
                new JObject { ["gespreksdeelname"] = medewerkerDeelname }
            )
        );
        Assert.Equal(gelezen1.Value<string>("uuid"), gelezen2.Value<string>("uuid"));
        Assert.Equal(medewerkerDeelname, gelezen1.Value<string>("gespreksdeelname"));
        Assert.Equal(bijdrage.Value<string>("url"), gelezen1.Value<string>("gespreksbijdrage"));

        // End, then no more bijdragen
        var ended = await SendAsync(HttpMethod.Post, $"/api/v1/gesprekken/{gesprekId}/eindigen", clientId);
        Assert.Equal(HttpStatusCode.OK, ended.StatusCode);
        Assert.NotNull((await ReadAsync(ended)).Value<string>("momentEindeGesprek"));

        var late = await SendAsync(
            HttpMethod.Post,
            "/api/v1/gespreksbijdragen",
            clientId,
            new JObject
            {
                ["gesprek"] = gesprekUrl,
                ["tekstGespreksBijdrage"] = "Nog iets",
                ["verzender"] = new JObject { ["gespreksdeelname"] = medewerkerDeelname },
            }
        );
        Assert.Equal(HttpStatusCode.BadRequest, late.StatusCode);
    }

    [Fact]
    public async Task The_other_aanleidingen_are_kept()
    {
        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body["aanleidingProductDienst"] = "https://producten.integrationtest.invalid/api/v1/producten/1";
        body["aanleidingAgendaAfspraak"] = "https://agenda.integrationtest.invalid/api/v1/afspraken/1";
        body["aanleidingPlan"] = "https://plannen.integrationtest.invalid/api/v1/plannen/1";
        ((JObject)body["ontvanger"]![0]!)["namens"] = "https://klantinteracties.integrationtest.invalid/api/v1/partijen/vertegenwoordigd";

        var clientId = NewClient();
        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var gesprek = await ReadAsync(response);
        Assert.Equal("https://plannen.integrationtest.invalid/api/v1/plannen/1", gesprek.Value<string>("aanleidingPlan"));
        var plan = await ReadAsync(
            await SendAsync(HttpMethod.Get, "/api/v1/gesprekken?aanleidingPlan=https://plannen.integrationtest.invalid/api/v1/plannen/1", clientId)
        );
        Assert.Contains(plan["results"]!, r => r.Value<string>("uuid") == gesprek.Value<string>("uuid"));
        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}", clientId));
        Assert.EndsWith("/vertegenwoordigd", detail["gespreksdeelnames"]!.First(d => d.Value<string>("type") == "partij").Value<string>("namens"));
        Assert.False(detail["gespreksbijdragen"]![0]!.Value<bool>("automatisch"));
        Assert.Equal("https://producten.integrationtest.invalid/api/v1/producten/1", gesprek.Value<string>("aanleidingProductDienst"));
        Assert.Equal("https://agenda.integrationtest.invalid/api/v1/afspraken/1", gesprek.Value<string>("aanleidingAgendaAfspraak"));
        Assert.Null(gesprek.Value<string>("aanleidingThema"));
    }

    [Fact]
    public async Task A_bijdrage_is_at_most_5000_characters()
    {
        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body["eersteBijdrage"]!["tekstGespreksBijdrage"] = new string('a', 5001);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", NewClient(), body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("tekstGespreksBijdrage", (await ReadAsync(response))["invalidParams"]!.Single().Value<string>("name"));
    }

    [Fact]
    public async Task A_reference_must_be_a_url_of_the_right_resource()
    {
        var clientId = NewClient();
        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(_factory.NewZaak())));

        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/gespreksbijdragen",
            clientId,
            new JObject
            {
                ["gesprek"] = gesprek.Value<string>("uuid"),
                ["tekstGespreksBijdrage"] = "Een UUID is geen verwijzing meer.",
                ["verzender"] = new JObject { ["nieuweDeelname"] = FlowBody.Deelname("partij") },
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("gesprek", (await ReadAsync(response))["invalidParams"]!.Single().Value<string>("name"));
    }

    [Fact]
    public async Task A_gesprek_on_a_closed_zaak_is_rejected()
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", NewClient(), FlowBody.NewGesprek(_factory.NewZaak(closed: true)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var param = (await ReadAsync(response))["invalidParams"]!.Single();
        Assert.Equal("aanleidingZaak", param.Value<string>("name"));
        Assert.Equal("zaak-gesloten", param.Value<string>("code"));
    }

    [Fact]
    public async Task The_first_bijdrage_needs_a_new_deelname_as_verzender()
    {
        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body["eersteBijdrage"]!["verzender"] = new JObject { ["gespreksdeelname"] = $"http://localhost/api/v1/gespreksdeelnames/{Guid.NewGuid()}" };

        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", NewClient(), body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_client_without_authorizations_is_forbidden()
    {
        var clientId = $"integration-test-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.GrantNoAuthorizations(clientId);

        var response = await SendAsync(HttpMethod.Get, "/api/v1/gesprekken", clientId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string clientId, JObject body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Authorization = TestIdentity.Create(clientId, Rsin);

        if (body != null)
        {
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
        }

        return await _client.SendAsync(request);
    }

    private static async Task<JObject> ReadAsync(HttpResponseMessage response) => JObject.Parse(await response.Content.ReadAsStringAsync());

    private string NewClient()
    {
        var clientId = $"integration-test-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.GrantAllAuthorizations(clientId);
        return clientId;
    }
}
