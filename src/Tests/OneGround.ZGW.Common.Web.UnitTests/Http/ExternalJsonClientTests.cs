using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

    private static ExternalJsonClient CreateClient(
        HttpMessageHandler handler,
        Action<ExternalJsonClientSettings> configure = null,
        IHttpContextAccessor httpContextAccessor = null,
        ILogger<ExternalJsonClient> logger = null,
        TimeProvider timeProvider = null
    )
    {
        var settings = new ExternalJsonClientSettings { AllowedHosts = ["api.example.test"] };
        configure?.Invoke(settings);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(ExternalJsonClient.HttpClientName)).Returns(() => new HttpClient(handler, disposeHandler: false));

        return new ExternalJsonClient(
            factory.Object,
            Options.Create(settings),
            logger ?? NullLogger<ExternalJsonClient>.Instance,
            httpContextAccessor,
            timeProvider
        );
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

    [Fact]
    public async Task GetJsonObjectAsync_SameUrlTwice_CallsOutOnce()
    {
        var handler = Respond(HttpStatusCode.OK, """{"naam":"Telefoon"}""");
        var client = CreateClient(handler);

        var first = await client.GetJsonObjectAsync(Url);
        var second = await client.GetJsonObjectAsync(Url);

        Assert.Equal(1, handler.Calls);
        Assert.Equal("Telefoon", (string)second["naam"]);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task GetJsonObjectAsync_ResultOfEarlierCallIsChanged_LaterCallIsNotAffected()
    {
        var client = CreateClient(Respond(HttpStatusCode.OK, """{"naam":"Telefoon"}"""));

        var first = await client.GetJsonObjectAsync(Url);
        first["naam"] = "gewijzigd";
        var second = await client.GetJsonObjectAsync(Url);

        Assert.Equal("Telefoon", (string)second["naam"]);
    }

    [Fact]
    public async Task GetJsonObjectAsync_FailedUrlTwice_CallsOutOnce()
    {
        var handler = Respond(HttpStatusCode.InternalServerError, "{}");
        var client = CreateClient(handler);

        Assert.Empty(await client.GetJsonObjectAsync(Url));
        Assert.Empty(await client.GetJsonObjectAsync(Url));

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_DifferentUrls_AreFetchedSeparately()
    {
        var handler = Respond(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);

        await client.GetJsonObjectAsync(Url);
        await client.GetJsonObjectAsync(Url + "?x=1");

        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_TimeBudgetOfRequestIsUsedUp_SkipsFurtherCalls()
    {
        var clock = new FakeClock();
        // Note: the first (external) call "takes" the whole budget of the request
        var handler = new StubHandler(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(10));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"naam":"x"}""") };
        });
        var client = CreateClient(handler, s => s.TotalTimeoutSeconds = 10, timeProvider: clock);

        var first = await client.GetJsonObjectAsync(Url);
        var second = await client.GetJsonObjectAsync(Url + "?andere=1");

        Assert.Equal("x", (string)first["naam"]);
        Assert.Empty(second);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_TimeBudgetUsedUp_LogsOnlyOnce()
    {
        var clock = new FakeClock();
        var logger = new CapturingLogger();
        var handler = new StubHandler(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(10));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = CreateClient(handler, s => s.TotalTimeoutSeconds = 10, logger: logger, timeProvider: clock);

        await client.GetJsonObjectAsync(Url);
        await client.GetJsonObjectAsync(Url + "?a=1");
        await client.GetJsonObjectAsync(Url + "?a=2");
        await client.GetJsonObjectAsync(Url + "?a=3");

        Assert.Equal(1, handler.Calls);
        Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task GetJsonObjectAsync_TimeBudgetNotUsedUp_StillCallsOut()
    {
        var clock = new FakeClock();
        var handler = new StubHandler(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(4));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var client = CreateClient(handler, s => s.TotalTimeoutSeconds = 10, timeProvider: clock);

        await client.GetJsonObjectAsync(Url);
        await client.GetJsonObjectAsync(Url + "?andere=1");

        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_TimedOutUrlTwice_CallsOutOnce()
    {
        var handler = new StubHandler(
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        );
        var client = CreateClient(handler, s => s.TimeoutSeconds = 1);

        Assert.Empty(await client.GetJsonObjectAsync(Url));
        Assert.Empty(await client.GetJsonObjectAsync(Url));

        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_ConcurrentCallsForOneUrl_CallOutOnce()
    {
        var release = new TaskCompletionSource();
        var handler = new StubHandler(
            async (_, _) =>
            {
                await release.Task;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"naam":"x"}""") };
            }
        );
        var client = CreateClient(handler);

        var calls = Enumerable.Range(0, 5).Select(_ => client.GetJsonObjectAsync(Url)).ToArray();
        release.SetResult();
        var results = await Task.WhenAll(calls);

        Assert.Equal(1, handler.Calls);
        Assert.All(results, r => Assert.Equal("x", (string)r["naam"]));
    }

    [Fact]
    public async Task GetJsonObjectAsync_ParallelCallsForOneUrl_CallOutOnce()
    {
        const int callers = 16;
        var release = new TaskCompletionSource();
        var started = new Barrier(callers);
        var handler = new StubHandler(
            async (_, _) =>
            {
                await release.Task;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"naam":"x"}""") };
            }
        );
        var client = CreateClient(handler);

        // Note: really parallel (threads released together by the barrier), unlike calls that are started one after the other
        var calls = Enumerable
            .Range(0, callers)
            .Select(_ =>
                Task.Run(() =>
                {
                    started.SignalAndWait();
                    return client.GetJsonObjectAsync(Url);
                })
            )
            .ToArray();
        await Task.Delay(100); // let every caller reach the shared fetch
        release.SetResult();
        var results = await Task.WhenAll(calls);

        Assert.Equal(1, handler.Calls);
        Assert.All(results, r => Assert.Equal("x", (string)r["naam"]));
    }

    [Fact]
    public async Task GetJsonObjectAsync_FetchFailsUnexpectedlyAfterItsOnlyCallerLeft_IsNotCached()
    {
        var release = new TaskCompletionSource();
        var attempt = 0;
        var handler = new StubHandler(
            async (_, _) =>
            {
                if (Interlocked.Increment(ref attempt) == 1)
                {
                    await release.Task;
                    throw new InvalidOperationException("boom");
                }

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"naam":"x"}""") };
            }
        );
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();

        var leaver = client.GetJsonObjectAsync(Url, cts.Token);
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => leaver);
        release.SetResult(); // the fetch now fails, with nobody waiting for it any more
        await Task.Delay(250); // the fetch removes itself on its own thread of continuations, give that time to finish

        var result = await client.GetJsonObjectAsync(Url);

        Assert.Equal("x", (string)result["naam"]);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_FirstCallerCancels_DoesNotFailAnotherCallerForTheSameUrl()
    {
        var release = new TaskCompletionSource();
        var handler = new StubHandler(
            async (_, _) =>
            {
                await release.Task;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"naam":"x"}""") };
            }
        );
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();

        var first = client.GetJsonObjectAsync(Url, cts.Token);
        var second = client.GetJsonObjectAsync(Url);
        await cts.CancelAsync();
        release.SetResult();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.Equal("x", (string)(await second)["naam"]);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_UnexpectedException_IsNotCached()
    {
        var attempt = 0;
        var handler = new StubHandler(_ =>
        {
            if (++attempt == 1)
                throw new InvalidOperationException("boom");

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"naam":"x"}""") };
        });
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetJsonObjectAsync(Url));
        var result = await client.GetJsonObjectAsync(Url);

        Assert.Equal("x", (string)result["naam"]);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetJsonObjectAsync_NoTokenGiven_UsesRequestAborted()
    {
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { RequestAborted = aborted.Token } };
        var handler = Respond(HttpStatusCode.OK, "{}");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateClient(handler, httpContextAccessor: accessor).GetJsonObjectAsync(Url));
    }

    [Fact]
    public async Task GetJsonObjectAsync_AddressBlocked_ReturnsEmptyObjectAndLogsOwnEvent()
    {
        var logger = new CapturingLogger();
        // Note: SocketsHttpHandler wraps what its ConnectCallback throws in an HttpRequestException.
        var handler = new StubHandler(_ => throw new HttpRequestException("connect failed", new BlockedAddressException("api.example.test")));

        var result = await CreateClient(handler, logger: logger).GetJsonObjectAsync(Url);

        Assert.Empty(result);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal("ExternalJsonAddressBlocked", entry.EventName);
        Assert.Equal(LogLevel.Warning, entry.Level);
    }

    [Fact]
    public async Task GetJsonObjectAsync_OrdinaryConnectionFailure_DoesNotLogTheBlockedEvent()
    {
        var logger = new CapturingLogger();
        var handler = new StubHandler(_ => throw new HttpRequestException("connection refused"));

        await CreateClient(handler, logger: logger).GetJsonObjectAsync(Url);

        Assert.DoesNotContain(logger.Entries, e => e.EventName == "ExternalJsonAddressBlocked");
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task GetJsonObjectAsync_SameRefusedUrlRepeatedly_LogsOnce()
    {
        var logger = new CapturingLogger();
        var client = CreateClient(Respond(HttpStatusCode.OK, "{}"), logger: logger);

        for (var i = 0; i < 5; i++)
            await client.GetJsonObjectAsync("https://andere-host.example.test/kanalen/1");

        Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
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

    private sealed class FakeClock : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }

    private sealed class CapturingLogger : ILogger<ExternalJsonClient>
    {
        public System.Collections.Generic.List<(LogLevel Level, string EventName)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // Note: debug entries are not interesting for these tests
            if (logLevel >= LogLevel.Information)
                Entries.Add((logLevel, eventId.Name));
        }
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
