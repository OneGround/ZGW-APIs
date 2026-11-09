using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Afsluiten;
using OneGround.ZGW.Gesprekken.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// A gesprek is a slow chat: the contactmoment exists once it is closed, by a deelnemer or after the term.
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class AfsluitenTests
{
    private const string Rsin = TestRsins.A;

    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AfsluitenTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task A_gesprek_has_no_contactmoment_until_it_is_closed()
    {
        var clientId = NewClient();
        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body["contactmoment"] = "https://klantinteracties.integrationtest.invalid/api/v1/klantcontacten/eigen";

        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, body));
        Assert.Null(gesprek.Value<string>("contactmoment"));

        var uuid = gesprek.Value<string>("uuid");
        var geeindigd = await ReadAsync(await SendAsync(HttpMethod.Post, $"/api/v1/gesprekken/{uuid}/eindigen", clientId));

        Assert.Equal($"https://klantinteracties.integrationtest.invalid/api/v1/klantcontacten/{uuid}", geeindigd.Value<string>("contactmoment"));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
        var beeindigd = await context.Events.SingleAsync(e => e.GesprekId == Guid.Parse(uuid) && e.Type == "nl.vng.gesprekken.gesprek.beeindigd");
        Assert.Equal(geeindigd.Value<string>("contactmoment"), JObject.Parse(beeindigd.Data).Value<string>("contactmoment"));
    }

    [Fact]
    public async Task A_gesprek_closes_three_days_after_the_last_bijdrage_of_the_gemeente()
    {
        var clientId = NewClient();
        var stil = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(_factory.NewZaak())));
        var actief = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(_factory.NewZaak())));

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
            var id = Guid.Parse(stil.Value<string>("uuid"));
            var oud = DateTime.UtcNow.AddDays(-4);
            await context.Gespreksbijdragen.Where(b => b.GesprekId == id).ExecuteUpdateAsync(u => u.SetProperty(b => b.Leveringsmoment, oud));
        }

        await _factory.Services.GetRequiredService<AutomatischSluitenService>().SluitVerlopenGesprekkenAsync(DateTime.UtcNow, CancellationToken.None);

        var gesloten = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{stil.Value<string>("uuid")}", clientId));
        Assert.NotNull(gesloten.Value<string>("momentEindeGesprek"));
        Assert.NotNull(gesloten.Value<string>("contactmoment"));

        var open = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{actief.Value<string>("uuid")}", clientId));
        Assert.Null(open.Value<string>("momentEindeGesprek"));

        using var check = _factory.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<GrcDbContext>();
        var gesprekId = Guid.Parse(stil.Value<string>("uuid"));
        var beeindigd = await db.Events.SingleAsync(e => e.GesprekId == gesprekId && e.Type == "nl.vng.gesprekken.gesprek.beeindigd");
        Assert.Contains("/gesprekken/", beeindigd.Subject);
    }

    [Fact]
    public async Task A_gesprek_waiting_for_the_gemeente_stays_open()
    {
        var clientId = NewClient();
        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(_factory.NewZaak())));
        var uuid = gesprek.Value<string>("uuid");
        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{uuid}", clientId));
        var inwoner = detail["gespreksdeelnames"]!.Single(d => d.Value<string>("type") == "partij").Value<string>("url");
        var vraag = new JObject
        {
            ["gesprek"] = gesprek.Value<string>("url"),
            ["tekstGespreksBijdrage"] = "Nog een vraag.",
            ["verzender"] = new JObject { ["gespreksdeelname"] = inwoner },
        };
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(HttpMethod.Post, "/api/v1/gespreksbijdragen", clientId, vraag)).StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
            var id = Guid.Parse(uuid);
            var oud = DateTime.UtcNow.AddDays(-4);
            await context.Gespreksbijdragen.Where(b => b.GesprekId == id).ExecuteUpdateAsync(u => u.SetProperty(b => b.Leveringsmoment, oud));
        }

        await _factory.Services.GetRequiredService<AutomatischSluitenService>().SluitVerlopenGesprekkenAsync(DateTime.UtcNow, CancellationToken.None);

        var open = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{uuid}", clientId));
        Assert.Null(open.Value<string>("momentEindeGesprek"));
    }

    [Fact]
    public async Task A_gesprek_needs_no_zaak()
    {
        var body = FlowBody.NewGesprek(null);
        body.Remove("aanleidingZaak");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", NewClient(), body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null((await ReadAsync(response)).Value<string>("aanleidingZaak"));
    }

    [Fact]
    public async Task A_gesprek_needs_a_first_bijdrage()
    {
        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body.Remove("eersteBijdrage");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", NewClient(), body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_list_shows_the_last_bijdrage()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();
        var body = FlowBody.NewGesprek(zaak);
        body["eersteBijdrage"]!["tekstGespreksBijdrage"] = new string('a', 300);
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, body);

        var lijst = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?aanleidingZaak={Uri.EscapeDataString(zaak)}", clientId));

        var laatste = lijst["results"]![0]!["laatsteBijdrage"]!;
        Assert.Equal(200, laatste.Value<string>("tekstFragment")!.Length);
        Assert.Equal(0, laatste.Value<int>("aantalBijlagen"));
        Assert.Contains("/gespreksdeelnames/", laatste.Value<string>("verzender"));
    }

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
