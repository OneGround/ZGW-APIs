using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Gesprekken.DataModel;
using OneGround.ZGW.Gesprekken.Web.Afsluiten;
using Xunit;

namespace OneGround.ZGW.Gesprekken.WebApi.UnitTests;

public class KlantcontactServiceTests
{
    private static readonly Gesprek Gesprek = new()
    {
        Id = Guid.NewGuid(),
        Referentie = "G2026-00000001",
        Gespreksonderwerp = "Onderwerp",
        MomentEindeGesprek = DateTime.UtcNow,
    };

    [Fact]
    public async Task Posts_a_klantcontact_without_the_text_of_the_gesprek_and_returns_its_url()
    {
        HttpRequestMessage sent = null;
        string body = null;
        var handler = new StubHandler(async r =>
        {
            sent = r;
            body = await r.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"url":"https://kic.example/api/v1/klantcontacten/1"}""", Encoding.UTF8, "application/json"),
            };
        });

        var url = await Service(handler, "https://kic.example/api/v1/").MaakAanAsync(Gesprek, CancellationToken.None);

        Assert.Equal("https://kic.example/api/v1/klantcontacten/1", url);
        Assert.Equal("https://kic.example/api/v1/klantcontacten", sent.RequestUri!.ToString());
        Assert.Equal("Token", sent.Headers.Authorization!.Scheme);
        Assert.Contains("MijnGesprekken", body);
        Assert.Contains("Onderwerp", body);
    }

    [Fact]
    public async Task Links_the_klantcontact_to_the_gesprek_the_zaak_and_the_deelnemers_of_this_register()
    {
        var posts = new List<(string Pad, string Body)>();
        var handler = new StubHandler(async r =>
        {
            posts.Add((r.RequestUri!.AbsolutePath, await r.Content!.ReadAsStringAsync()));
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"uuid":"11111111-1111-1111-1111-111111111111","url":"https://kic.example/api/v1/klantcontacten/1"}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });
        var gesprek = new Gesprek
        {
            Id = Guid.NewGuid(),
            Referentie = "G1",
            Gespreksonderwerp = "Onderwerp",
            AanleidingZaak = "https://zaken.example/api/v1/zaken/1",
            MomentEindeGesprek = DateTime.UtcNow,
            Deelnames =
            [
                new Gespreksdeelname
                {
                    Type = DeelnameType.partij,
                    Naam = "Inwoner",
                    Partij = "https://kic.example/api/v1/partijen/22222222-2222-2222-2222-222222222222",
                },
                new Gespreksdeelname
                {
                    Type = DeelnameType.actor,
                    Naam = "Medewerker",
                    Actor = "https://kic.example/api/v1/actoren/33333333-3333-3333-3333-333333333333",
                },
                new Gespreksdeelname
                {
                    Type = DeelnameType.actor,
                    Naam = "Systeem",
                    Actor = "https://elders.example/actoren/44444444-4444-4444-4444-444444444444",
                },
            ],
        };

        await Service(handler, "https://kic.example/api/v1").MaakAanAsync(gesprek, CancellationToken.None);

        Assert.Equal(
            [
                "/api/v1/klantcontacten",
                "/api/v1/onderwerpobjecten",
                "/api/v1/onderwerpobjecten",
                "/api/v1/betrokkenen",
                "/api/v1/actorklantcontacten",
            ],
            posts.Select(p => p.Pad).ToArray()
        );
        Assert.Contains("https://grc.example/api/v1/gesprekken/1", posts[1].Body);
        Assert.Contains("https://zaken.example/api/v1/zaken/1", posts[2].Body);
        Assert.Contains("22222222-2222-2222-2222-222222222222", posts[3].Body);
        Assert.Contains("11111111-1111-1111-1111-111111111111", posts[3].Body);
        Assert.Contains("33333333-3333-3333-3333-333333333333", posts[4].Body);
    }

    [Fact]
    public async Task A_failing_link_keeps_the_klantcontact()
    {
        var handler = new StubHandler(r =>
            r.RequestUri!.AbsolutePath.EndsWith("/klantcontacten")
                ? Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(
                            """{"uuid":"11111111-1111-1111-1111-111111111111","url":"https://kic.example/api/v1/klantcontacten/1"}""",
                            Encoding.UTF8,
                            "application/json"
                        ),
                    }
                )
                : throw new HttpRequestException("niet bereikbaar")
        );

        var url = await Service(handler, "https://kic.example/api/v1").MaakAanAsync(Gesprek, CancellationToken.None);

        Assert.Equal("https://kic.example/api/v1/klantcontacten/1", url);
    }

    [Fact]
    public async Task Without_klantinteracties_nothing_is_sent()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("no call expected"));

        Assert.Null(await Service(handler, null).MaakAanAsync(Gesprek, CancellationToken.None));
    }

    [Fact]
    public async Task A_time_out_does_not_stop_closing_the_gesprek()
    {
        var handler = new StubHandler(_ => throw new TaskCanceledException("time-out"));

        Assert.Null(await Service(handler, "https://kic.example/api/v1").MaakAanAsync(Gesprek, CancellationToken.None));
    }

    [Fact]
    public async Task An_unreadable_answer_does_not_stop_closing_the_gesprek()
    {
        var handler = new StubHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("<html>", Encoding.UTF8, "text/html") })
        );

        Assert.Null(await Service(handler, "https://kic.example/api/v1").MaakAanAsync(Gesprek, CancellationToken.None));
    }

    [Fact]
    public async Task A_refusal_does_not_stop_closing_the_gesprek()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        Assert.Null(await Service(handler, "https://kic.example/api/v1").MaakAanAsync(Gesprek, CancellationToken.None));
    }

    private static KlantinteractiesKlantcontactService Service(HttpMessageHandler handler, string api)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "Klantinteracties:Api", api }, { "Klantinteracties:Token", "geheim" } })
            .Build();
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(u => u.GetUri(It.IsAny<IUrlEntity>())).Returns("https://grc.example/api/v1/gesprekken/1");
        return new KlantinteractiesKlantcontactService(
            new HttpClient(handler),
            configuration,
            uriService.Object,
            NullLogger<KlantinteractiesKlantcontactService>.Instance
        );
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
