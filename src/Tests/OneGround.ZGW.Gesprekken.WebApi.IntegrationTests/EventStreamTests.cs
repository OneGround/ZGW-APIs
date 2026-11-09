using System;
using System.Collections.Generic;
using System.IO;
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
using OneGround.ZGW.Gesprekken.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// The event log (outbox) and GET /events, against the real API and PostgreSQL: snapshot with Feed-Cursor, follow,
/// resume with Last-Event-ID, 410 for a cursor that is too old.
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class EventStreamTests
{
    private const string Rsin = TestRsins.A;

    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EventStreamTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task A_snapshot_cursor_followed_by_the_stream_yields_every_change_after_it()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();

        var snapshot = await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken?aanleidingZaak={Uri.EscapeDataString(zaak)}", clientId);
        var cursor = snapshot.Headers.GetValues("Feed-Cursor").Single();

        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak)));
        await SendAsync(HttpMethod.Post, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}/eindigen", clientId);

        var events = await ReadEventsAsync(clientId, cursor, count: 5, $"aanleidingZaak={Uri.EscapeDataString(zaak)}");

        Assert.Equal(
            [
                "nl.vng.gesprekken.gesprek.aangemaakt",
                "nl.vng.gesprekken.gespreksdeelname.aangemaakt",
                "nl.vng.gesprekken.gespreksdeelname.aangemaakt",
                "nl.vng.gesprekken.gespreksbijdrage.aangemaakt",
                "nl.vng.gesprekken.gesprek.beeindigd",
            ],
            events.Select(e => e.Value<string>("type")).ToArray()
        );
        Assert.All(events, e => Assert.Equal(gesprek.Value<string>("url"), e.Value<string>("subject")));
        Assert.All(events, e => Assert.Equal("1.0", e.Value<string>("specversion")));
        Assert.All(events, e => Assert.Equal(zaak, e.Value<string>("aanleidingzaak")));
        Assert.Equal(
            events.Select(e => long.Parse(e.Value<string>("sequence"))).OrderBy(s => s),
            events.Select(e => long.Parse(e.Value<string>("sequence")))
        );
        Assert.Equal(gesprek.Value<string>("url"), events[0]["data"]!.Value<string>("url"));

        // Deelname events refer to the gesprek and the partij or actor
        var partijDeelname = events.Single(e =>
            e.Value<string>("type")!.Contains("gespreksdeelname") && e["data"]!.Value<string>("type") == "partij"
        );
        Assert.Equal(gesprek.Value<string>("url"), partijDeelname["data"]!.Value<string>("gesprek"));
        Assert.Equal(FlowBody.Partij, partijDeelname["data"]!.Value<string>("partij"));
    }

    [Fact]
    public async Task Reconnecting_with_the_last_seen_id_resumes_without_gaps_or_repeats()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();
        var cursor = await HeadAsync(clientId);
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak));

        var all = await ReadEventsAsync(clientId, cursor, count: 4, $"aanleidingZaak={Uri.EscapeDataString(zaak)}");
        var resumed = await ReadEventsAsync(clientId, all[1].Value<string>("sequence"), count: 2, $"aanleidingZaak={Uri.EscapeDataString(zaak)}");

        Assert.Equal(all.Skip(2).Select(e => e.Value<string>("id")), resumed.Select(e => e.Value<string>("id")));
    }

    [Fact]
    public async Task A_rejected_change_writes_no_event()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak(closed: true);
        var before = await HeadAsync(clientId);

        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
        Assert.False(await context.Events.AnyAsync(e => e.AanleidingZaak == zaak));
        Assert.True(long.Parse(await HeadAsync(clientId)) >= long.Parse(before));
    }

    [Fact]
    public async Task A_cursor_before_the_oldest_available_event_is_gone()
    {
        // A tenant of its own, so removing old events does not affect other tests
        const string rsin = TestRsins.C;
        var clientId = NewClient();
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(_factory.NewZaak()), rsin);
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(_factory.NewZaak()), rsin);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
            var oldest = await context.Events.Where(e => e.Owner == rsin).OrderBy(e => e.Sequence).Take(2).ToListAsync();
            context.Events.RemoveRange(oldest);
            await context.SaveChangesAsync();
        }

        var response = await SendAsync(HttpMethod.Get, "/api/v1/events", clientId, rsin: rsin, lastEventId: "0");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Equal("cursor-verlopen", (await ReadAsync(response)).Value<string>("code"));
    }

    [Fact]
    public async Task A_last_event_id_that_is_not_a_cursor_is_a_bad_request()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/events", NewClient(), lastEventId: "abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_stream_only_has_events_of_the_tenant()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();
        var cursor = await HeadAsync(clientId, TestRsins.D);
        await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak), TestRsins.B);

        var events = await ReadEventsAsync(
            clientId,
            cursor,
            count: 1,
            $"aanleidingZaak={Uri.EscapeDataString(zaak)}",
            TestRsins.D,
            TimeSpan.FromSeconds(3)
        );

        Assert.Empty(events);
    }

    private async Task<string> HeadAsync(string clientId, string rsin = Rsin)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/gesprekken?referentie=geen", clientId, rsin: rsin);
        return response.Headers.GetValues("Feed-Cursor").Single();
    }

    private async Task<List<JObject>> ReadEventsAsync(
        string clientId,
        string lastEventId,
        int count,
        string query,
        string rsin = Rsin,
        TimeSpan? timeout = null
    )
    {
        using var cancel = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        var request = NewRequest(HttpMethod.Get, $"/api/v1/events?{query}", clientId, rsin);
        request.Headers.Add("Last-Event-ID", lastEventId);

        var events = new List<JObject>();
        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel.Token);
            Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancel.Token));

            string id = null;
            while (events.Count < count && await reader.ReadLineAsync(cancel.Token) is { } line)
            {
                if (line.StartsWith("id: "))
                    id = line[4..];
                else if (line.StartsWith("data: "))
                {
                    var cloudEvent = JObject.Parse(line[6..]);
                    Assert.Equal(id, cloudEvent.Value<string>("sequence"));
                    events.Add(cloudEvent);
                }
            }
        }
        catch (OperationCanceledException) when (timeout != null)
        {
            // Expected when waiting for events that should not arrive
        }

        return events;
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string path, string clientId, string rsin)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Authorization = TestIdentity.Create(clientId, rsin);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        string clientId,
        JObject body = null,
        string rsin = Rsin,
        string lastEventId = null
    )
    {
        var request = NewRequest(method, path, clientId, rsin);
        if (lastEventId != null)
            request.Headers.Add("Last-Event-ID", lastEventId);
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
