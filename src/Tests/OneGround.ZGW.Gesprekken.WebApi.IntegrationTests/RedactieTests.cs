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
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Controllers;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Authentication;
using Xunit;
using AutorisatieResponseDto = OneGround.ZGW.Autorisaties.Contracts.v1.Responses.AutorisatieResponseDto;

namespace OneGround.ZGW.Gesprekken.WebApi.IntegrationTests;

/// <summary>
/// POST /gespreksbijdragen/{uuid}/redigeren: beheer removes a bijdrage, also from the event history, and the stream
/// tells consumers which events to forget.
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class RedactieTests
{
    private const string Rsin = TestRsins.A;

    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RedactieTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task A_redacted_bijdrage_is_gone_from_the_gesprek_and_the_stream()
    {
        var clientId = NewClient();
        var zaak = _factory.NewZaak();
        var cursor = (await SendAsync(HttpMethod.Get, "/api/v1/gesprekken?referentie=geen", clientId)).Headers.GetValues("Feed-Cursor").Single();

        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, FlowBody.NewGesprek(zaak)));
        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}", clientId));
        var bijdrage = detail["gespreksbijdragen"]!.Single();
        var deelname = detail["gespreksdeelnames"]!.First(d => d.Value<string>("type") == "partij").Value<string>("url");
        await SendAsync(
            HttpMethod.Post,
            $"/api/v1/gespreksbijdragen/{bijdrage.Value<string>("uuid")}/gelezen",
            clientId,
            new JObject { ["gespreksdeelname"] = deelname }
        );

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/gespreksbijdragen/{bijdrage.Value<string>("uuid")}/redigeren",
            clientId,
            new JObject { ["reden"] = "Bevatte gegevens van een derde." }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var redactie = await ReadAsync(response);
        Assert.Equal(bijdrage.Value<string>("url"), redactie.Value<string>("url"));
        var sequences = redactie["geredigeerdeSequences"]!.Select(s => s.Value<string>()).ToList();
        Assert.Equal(2, sequences.Count);

        var after = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}", clientId));
        Assert.Empty(after["gespreksbijdragen"]!);

        // The stream skips the redacted events and ends with the redactie; no text of the bijdrage is left in it
        var events = await ReadEventsAsync(clientId, cursor, count: 4, $"aanleidingZaak={Uri.EscapeDataString(zaak)}");
        Assert.Equal(
            [
                "nl.vng.gesprekken.gesprek.aangemaakt",
                "nl.vng.gesprekken.gespreksdeelname.aangemaakt",
                "nl.vng.gesprekken.gespreksdeelname.aangemaakt",
                "nl.vng.gesprekken.gespreksbijdrage.geredigeerd",
            ],
            events.Select(e => e.Value<string>("type")).ToArray()
        );
        Assert.Equal(sequences, events[^1]["data"]!["geredigeerdeSequences"]!.Select(s => s.Value<string>()));
        Assert.DoesNotContain(events, e => e.ToString().Contains(bijdrage.Value<string>("tekstGespreksBijdrage")!));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GrcDbContext>();
        var stored = await context.Events.Where(e => sequences.Contains(e.Sequence.ToString())).ToListAsync();
        Assert.All(stored, e => Assert.True(e.Geredigeerd));
        Assert.All(stored, e => Assert.Equal("{}", e.Data));
    }

    [Fact]
    public async Task Redacting_needs_the_redigeren_scope()
    {
        var beheer = NewClient();
        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", beheer, FlowBody.NewGesprek(_factory.NewZaak())));
        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}", beheer));
        var bijdrage = detail["gespreksbijdragen"]!.Single().Value<string>("uuid");

        var deelnemer = $"integration-test-{Guid.NewGuid():N}";
        _factory.AutorisatiesApi.Grant(
            deelnemer,
            new AutorisatieResponseDto
            {
                Component = "grc",
                Scopes = [AuthorizationScopes.Gesprekken.Read, AuthorizationScopes.Gesprekken.Create, AuthorizationScopes.Gesprekken.Update],
            }
        );

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/gespreksbijdragen/{bijdrage}/redigeren",
            deelnemer,
            new JObject { ["reden"] = "Mag niet." }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_reason_is_required()
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/v1/gespreksbijdragen/{Guid.NewGuid()}/redigeren", NewClient(), new JObject());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<List<JObject>> ReadEventsAsync(string clientId, string lastEventId, int count, string query)
    {
        using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var request = NewRequest(HttpMethod.Get, $"/api/v1/events?{query}", clientId);
        request.Headers.Add("Last-Event-ID", lastEventId);

        var events = new List<JObject>();
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel.Token);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancel.Token));
        while (events.Count < count && await reader.ReadLineAsync(cancel.Token) is { } line)
        {
            if (line.StartsWith("data: "))
                events.Add(JObject.Parse(line[6..]));
        }

        return events;
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string path, string clientId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("Api-Version", Api.LatestVersion_1_0);
        request.Headers.Authorization = TestIdentity.Create(clientId, Rsin);
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string clientId, JObject body = null)
    {
        var request = NewRequest(method, path, clientId);
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
