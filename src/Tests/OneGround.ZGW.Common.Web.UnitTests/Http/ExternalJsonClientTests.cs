using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Configuration;
using OneGround.ZGW.Common.Web.Http;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Http;

public class ExternalJsonClientTests
{
    private const string Url = "https://api.example.test/kanalen/1";

    private static ExternalJsonClient CreateClient(HttpMessageHandler handler, Action<ExternalJsonClientSettings> configure = null)
    {
        var settings = new ExternalJsonClientSettings { AllowedHosts = ["api.example.test"] };
        configure?.Invoke(settings);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(ExternalJsonClient.HttpClientName)).Returns(() => new HttpClient(handler, disposeHandler: false));

        return new ExternalJsonClient(factory.Object, Options.Create(settings), NullLogger<ExternalJsonClient>.Instance);
    }

    private static StubHandler Respond(HttpStatusCode status, string body, string contentType = "application/json") =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, contentType) });

    [Fact]
    public async Task GetJsonObjectAsync_ValidJsonObject_ReturnsIt()
    {
        var handler = Respond(HttpStatusCode.OK, """{"naam":"Telefoon","code":7,"nested":{"a":[1,2]}}""");

        var result = await CreateClient(handler).GetJsonObjectAsync(Url);

        Assert.Equal("Telefoon", (string)result["naam"]);
        Assert.Equal(7, (int)result["code"]);
        Assert.Equal(2, result["nested"]["a"].Count());
    }

    [Fact]
    public async Task GetJsonObjectAsync_DateLikeStrings_AreNotReformatted()
    {
        var handler = Respond(HttpStatusCode.OK, """{"begin":"2020-01-31T00:00:00Z","dag":"2020-01-31"}""");

        var result = await CreateClient(handler).GetJsonObjectAsync(Url);

        Assert.Equal(JTokenType.String, result["begin"].Type);
        Assert.Equal("2020-01-31T00:00:00Z", (string)result["begin"]);
        Assert.Equal("2020-01-31", (string)result["dag"]);
    }

    [Fact]
    public async Task GetJsonObjectAsync_SendsNoAuthorizationAndAcceptsJson()
    {
        HttpRequestMessage seen = null;
        var handler = new StubHandler(r =>
        {
            seen = r;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        await CreateClient(handler).GetJsonObjectAsync(Url);

        Assert.Null(seen.Headers.Authorization);
        Assert.Contains(seen.Headers.Accept, h => h.MediaType == "application/json");
        Assert.Equal(HttpMethod.Get, seen.Method);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Redirect)]
    public async Task GetJsonObjectAsync_NonSuccessStatus_ReturnsEmptyObject(HttpStatusCode status)
    {
        var result = await CreateClient(Respond(status, """{"naam":"x"}""")).GetJsonObjectAsync(Url);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("niet-json")]
    [InlineData("")]
    [InlineData("""{"onvolledig":""")]
    [InlineData("[1,2,3]")]
    [InlineData("\"tekst\"")]
    [InlineData("42")]
    [InlineData("null")]
    public async Task GetJsonObjectAsync_BodyIsNotAJsonObject_ReturnsEmptyObject(string body)
    {
        var result = await CreateClient(Respond(HttpStatusCode.OK, body)).GetJsonObjectAsync(Url);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJsonObjectAsync_HttpRequestFails_ReturnsEmptyObject()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await CreateClient(handler).GetJsonObjectAsync(Url);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJsonObjectAsync_ResponseExceedsMaxBytes_ReturnsEmptyObject()
    {
        var body = """{"x":"%s"}""".Replace("%s", new string('a', 2000));

        var result = await CreateClient(Respond(HttpStatusCode.OK, body), s => s.MaxResponseBytes = 1000).GetJsonObjectAsync(Url);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJsonObjectAsync_ResponseExceedsMaxBytesWithoutContentLength_ReturnsEmptyObject()
    {
        var body = Encoding.UTF8.GetBytes("""{"x":"%s"}""".Replace("%s", new string('a', 2000)));
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new NoLengthContent(body) });

        var result = await CreateClient(handler, s => s.MaxResponseBytes = 1000).GetJsonObjectAsync(Url);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJsonObjectAsync_Timeout_ReturnsEmptyObject()
    {
        var handler = new StubHandler(
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }
        );

        var result = await CreateClient(handler, s => s.TimeoutSeconds = 1).GetJsonObjectAsync(Url);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetJsonObjectAsync_CallerCancels_Propagates()
    {
        using var cts = new CancellationTokenSource();
        var handler = new StubHandler(
            async (_, ct) =>
            {
                await cts.CancelAsync();
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        );

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateClient(handler).GetJsonObjectAsync(Url, cts.Token));
    }

    [Theory]
    [InlineData("http://api.example.test/kanalen/1")] // not https
    [InlineData("https://andere-host.example.test/kanalen/1")] // not in allowlist
    [InlineData("niet-een-url")]
    public async Task GetJsonObjectAsync_UrlNotAllowed_ReturnsEmptyObjectWithoutCalling(string url)
    {
        var handler = Respond(HttpStatusCode.OK, """{"naam":"x"}""");

        var result = await CreateClient(handler).GetJsonObjectAsync(url);

        Assert.Empty(result);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetJsonObjectAsync_NoUrl_ReturnsEmptyObjectWithoutCalling(string url)
    {
        var handler = Respond(HttpStatusCode.OK, """{"naam":"x"}""");

        var result = await CreateClient(handler).GetJsonObjectAsync(url);

        Assert.Empty(result);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_EmptyAllowlist_RefusesEverything()
    {
        var handler = Respond(HttpStatusCode.OK, """{"naam":"x"}""");

        var result = await CreateClient(handler, s => s.AllowedHosts = []).GetJsonObjectAsync(Url);

        Assert.Empty(result);
        Assert.Equal(0, handler.Calls);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
            : this((r, _) => Task.FromResult(respond(r))) { }

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return _respond(request, cancellationToken);
        }
    }

    // Content that does not know its own length, so the size limit has to be enforced while reading.
    private sealed class NoLengthContent : HttpContent
    {
        private readonly byte[] _bytes;

        public NoLengthContent(byte[] bytes)
        {
            _bytes = bytes;
        }

        protected override Task SerializeToStreamAsync(System.IO.Stream stream, TransportContext context) => stream.WriteAsync(_bytes).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
