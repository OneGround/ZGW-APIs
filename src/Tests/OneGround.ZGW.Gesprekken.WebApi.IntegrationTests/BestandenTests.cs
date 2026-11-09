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
/// Bijlagen via patterns/file-upload: start an upload, send the file to the storage, refer to the bestand.
/// </summary>
[Collection(GesprekkenApiCollection.Name)]
public class BestandenTests
{
    private const string Rsin = TestRsins.A;

    private readonly GesprekkenWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BestandenTests(GesprekkenApiFixture fixture)
    {
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task An_uploaded_bestand_can_be_a_bijlage_of_the_first_bijdrage_and_is_shown_without_extra_calls()
    {
        var clientId = NewClient();
        var sessie = await StartUploadAsync(clientId);
        Assert.Equal("uploadInAfwachting", sessie["bestand"]!.Value<string>("status"));

        var bestandUrl = sessie["bestand"]!.Value<string>("url");
        var path = new Uri(bestandUrl).PathAndQuery;

        var teVroeg = FlowBody.NewGesprek(_factory.NewZaak());
        teVroeg["eersteBijdrage"]!["bijlagen"] = new JArray(new JObject { ["bestand"] = bestandUrl });
        var geweigerd = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, teVroeg);
        Assert.Equal(HttpStatusCode.BadRequest, geweigerd.StatusCode);
        Assert.Equal("eersteBijdrage.bijlagen.0.bestand", (await ReadAsync(geweigerd))["invalidParams"]!.Single().Value<string>("name"));

        _factory.Opslag.Upload(sessie.Value<string>("uploadUrl"), 1024);

        var bestand = await ReadAsync(await SendAsync(HttpMethod.Get, path, clientId));
        Assert.Equal("beschikbaar", bestand.Value<string>("status"));
        Assert.Contains("signature=download", bestand.Value<string>("downloadUrl"));

        var created = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, teVroeg);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var detail = await ReadAsync(
            await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{(await ReadAsync(created)).Value<string>("uuid")}", clientId)
        );
        var bijlage = detail["gespreksbijdragen"]![0]!["bijlagen"]![0]!;
        Assert.Equal(bestandUrl, bijlage.Value<string>("bestand"));
        Assert.Equal("loonstrook.pdf", bijlage.Value<string>("bestandsnaam"));
        Assert.Equal("application/pdf", bijlage.Value<string>("contentType"));
        Assert.Equal(1024, bijlage.Value<long>("bestandsomvang"));

        // A bestand belongs to one bijdrage
        var nogEens = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, teVroeg);
        Assert.Equal(HttpStatusCode.BadRequest, nogEens.StatusCode);
    }

    [Fact]
    public async Task An_upload_of_another_size_is_refused_and_removed()
    {
        var clientId = NewClient();
        var sessie = await StartUploadAsync(clientId);

        _factory.Opslag.Upload(sessie.Value<string>("uploadUrl"), 9999);

        var bestand = await ReadAsync(await SendAsync(HttpMethod.Get, new Uri(sessie["bestand"]!.Value<string>("url")).PathAndQuery, clientId));
        Assert.Equal("geweigerd", bestand.Value<string>("status"));
        Assert.Null(bestand["downloadUrl"]);
        Assert.DoesNotContain(_factory.Opslag.Objecten.Keys, k => k.EndsWith(sessie.Value<string>("fileId")));
    }

    [Fact]
    public async Task A_bestand_that_is_too_large_is_refused()
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/bestanden/upload-sessie",
            NewClient(),
            new JObject { ["bestandsnaam"] = "groot.pdf", ["bestandsomvang"] = 100L * 1024 * 1024 }
        );

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task The_same_bestand_twice_in_one_bijdrage_is_refused()
    {
        var clientId = NewClient();
        var sessie = await StartUploadAsync(clientId);
        _factory.Opslag.Upload(sessie.Value<string>("uploadUrl"), 1024);
        var bestandUrl = sessie["bestand"]!.Value<string>("url");

        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body["eersteBijdrage"]!["bijlagen"] = new JArray(new JObject { ["bestand"] = bestandUrl }, new JObject { ["bestand"] = bestandUrl });
        var response = await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("eersteBijdrage.bijlagen.1.bestand", (await ReadAsync(response))["invalidParams"]!.Single().Value<string>("name"));
    }

    [Fact]
    public async Task Redacting_a_bijdrage_removes_its_bestanden()
    {
        var clientId = NewClient();
        var sessie = await StartUploadAsync(clientId);
        _factory.Opslag.Upload(sessie.Value<string>("uploadUrl"), 1024);

        var body = FlowBody.NewGesprek(_factory.NewZaak());
        body["eersteBijdrage"]!["bijlagen"] = new JArray(new JObject { ["bestand"] = sessie["bestand"]!.Value<string>("url") });
        var gesprek = await ReadAsync(await SendAsync(HttpMethod.Post, "/api/v1/gesprekken", clientId, body));
        var detail = await ReadAsync(await SendAsync(HttpMethod.Get, $"/api/v1/gesprekken/{gesprek.Value<string>("uuid")}", clientId));

        var response = await SendAsync(
            HttpMethod.Post,
            $"/api/v1/gespreksbijdragen/{detail["gespreksbijdragen"]![0]!.Value<string>("uuid")}/redigeren",
            clientId,
            new JObject { ["reden"] = "Verkeerde bijlage." }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(_factory.Opslag.Objecten.Keys, k => k.EndsWith(sessie.Value<string>("fileId")));
        var weg = await SendAsync(HttpMethod.Get, new Uri(sessie["bestand"]!.Value<string>("url")).PathAndQuery, clientId);
        Assert.Equal(HttpStatusCode.NotFound, weg.StatusCode);
    }

    private async Task<JObject> StartUploadAsync(string clientId)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/api/v1/bestanden/upload-sessie",
            clientId,
            new JObject
            {
                ["bestandsnaam"] = "loonstrook.pdf",
                ["bestandsomvang"] = 1024,
                ["contentType"] = "application/pdf",
            }
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync(response);
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
