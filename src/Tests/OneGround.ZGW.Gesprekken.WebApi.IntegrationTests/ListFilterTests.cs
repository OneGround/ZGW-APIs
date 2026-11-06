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
/// GET /gesprekken: the deelname filters and cursor pagination (patterns/pagination, next).
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class ListFilterTests
{
    private const string Rsin = TestRsins.A;

    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ListFilterTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task The_deelname_filters_select_on_the_deelnames_of_a_gesprek()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();

        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak)));
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak));

        var zaakFilter = $"aanleidingZaak={Uri.EscapeDataString(zaak)}";
        Assert.Equal(2, await CountAsync(clientId, $"{zaakFilter}&gespreksdeelname__type=actor"));

        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}", clientId));
        var deelname = detail["gespreksdeelnames"]![0]!.Value<string>("url");
        var byDeelname = await ReadAsync(
            await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?gespreksdeelname={Uri.EscapeDataString(deelname)}", clientId)
        );
        Assert.Equal(gesprek.Value<string>("uuid"), byDeelname["results"]!.Single().Value<string>("uuid"));

        Assert.Equal(2, await CountAsync(clientId, $"{zaakFilter}&gespreksdeelname__partij={Uri.EscapeDataString(FlowBody.Partij)}"));
    }

    [Fact]
    public async Task Following_next_returns_every_gesprek_once_newest_first()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();
        for (var i = 0; i < 5; i++)
            await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak));

        var first = await ReadAsync(
            await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?aanleidingZaak={Uri.EscapeDataString(zaak)}&pageSize=2", clientId)
        );
        Assert.Equal(5, first.Value<int>("count"));
        Assert.Equal(2, first["results"]!.Count());
        Assert.Null(first["previous"]);
        Assert.Contains("pageSize=2", first.Value<string>("next"));
        Assert.Contains("cursor=", first.Value<string>("next"));

        var seen = first["results"]!.Select(r => r.Value<string>("uuid")).ToList();
        var momenten = first["results"]!.Select(r => r.Value<DateTime>("aanvangsmomentGesprek")).ToList();
        var next = first.Value<string>("next");

        // A gesprek added while paging is newer than the cursor, so it does not shift the pages that follow
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak));

        while (next != null)
        {
            var page = await ReadAsync(await SendAsync(HttpMethod.Get, new Uri(next).PathAndQuery, clientId));
            seen.AddRange(page["results"]!.Select(r => r.Value<string>("uuid")));
            momenten.AddRange(page["results"]!.Select(r => r.Value<DateTime>("aanvangsmomentGesprek")));
            next = page.Value<string>("next");
        }

        Assert.Equal(5, seen.Count);
        Assert.Equal(5, seen.Distinct().Count());
        Assert.Equal(momenten.OrderByDescending(m => m), momenten);
    }

    [Fact]
    public async Task A_cursor_that_is_not_ours_is_a_bad_request()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/gesprekken?cursor=bm9n", NewClient());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("cursor", (await ReadAsync(response))["invalidParams"]!.Single().Value<string>("name"));
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("gespreksdeelname__type=medewerker")]
    [InlineData("gespreksdeelname=550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("gespreksdeelname__partij=geen-url")]
    public async Task Invalid_filter_values_are_a_bad_request(string query)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?{query}", NewClient());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> CountAsync(string clientId, string query) =>
        (await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?{query}", clientId))).Value<int>("count");

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string clientId, JObject body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Authorization = TestIdentity.Create(clientId, Rsin);
        if (body != null)
            request.Content = new StringContent(body.ToString(), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
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
