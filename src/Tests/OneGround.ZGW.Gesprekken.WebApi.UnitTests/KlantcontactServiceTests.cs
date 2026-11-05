using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
    public async Task Without_klantinteracties_nothing_is_sent()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("no call expected"));

        Assert.Null(await Service(handler, null).MaakAanAsync(Gesprek, CancellationToken.None));
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
        return new KlantinteractiesKlantcontactService(
            new HttpClient(handler),
            configuration,
            NullLogger<KlantinteractiesKlantcontactService>.Instance
        );
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
    }
}
