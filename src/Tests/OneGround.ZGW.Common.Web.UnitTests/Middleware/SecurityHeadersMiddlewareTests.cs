using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using OneGround.ZGW.Common.Web.Middleware;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private const string XFrameOptions = "X-Frame-Options";
    private const string ContentSecurityPolicy = "Content-Security-Policy";

    [Fact]
    public async Task Sets_both_security_headers_exactly_once_on_a_successful_response()
    {
        var (context, response) = CreateHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        // The middleware defers to OnStarting; nothing should be written until the response actually starts.
        Assert.False(context.Response.Headers.ContainsKey(XFrameOptions));
        Assert.False(context.Response.Headers.ContainsKey(ContentSecurityPolicy));

        await response.FireOnStartingAsync();

        AssertSingleHeader(context, XFrameOptions, "DENY");
        AssertSingleHeader(context, ContentSecurityPolicy, "frame-ancestors 'none'");
    }

    [Fact]
    public async Task Keeps_both_security_headers_on_an_exception_handled_response()
    {
        var (context, response) = CreateHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => throw new InvalidOperationException("boom"));

        // The middleware registers OnStarting before calling next, and must not swallow the exception.
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        // Mirror UseExceptionHandler("/error"): the failed response is cleared and re-executed as a 500
        // before it starts. The OnStarting callback registered above must re-apply the headers.
        context.Response.Headers.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await response.FireOnStartingAsync();

        AssertSingleHeader(context, XFrameOptions, "DENY");
        AssertSingleHeader(context, ContentSecurityPolicy, "frame-ancestors 'none'");
    }

    private static void AssertSingleHeader(HttpContext context, string name, string expectedValue)
    {
        Assert.True(context.Response.Headers.TryGetValue(name, out var values), $"Expected header '{name}' to be present.");
        Assert.Equal(1, values.Count);
        Assert.Equal(expectedValue, values.ToString());
    }

    private static (HttpContext context, RecordingResponseFeature response) CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var response = new RecordingResponseFeature();
        context.Features.Set<IHttpResponseFeature>(response);
        return (context, response);
    }

    // DefaultHttpContext's built-in response feature treats OnStarting as a no-op, so a plain
    // DefaultHttpContext can never fire the callback the middleware relies on. This feature records the
    // callbacks and lets the test invoke them the way Kestrel does (in reverse registration order).
    private sealed class RecordingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> callback, object state)> _onStarting = new();

        public int StatusCode { get; set; } = StatusCodes.Status200OK;
        public string ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

        public void OnCompleted(Func<object, Task> callback, object state) { }

        public async Task FireOnStartingAsync()
        {
            HasStarted = true;
            for (var i = _onStarting.Count - 1; i >= 0; i--)
            {
                await _onStarting[i].callback(_onStarting[i].state);
            }
        }
    }
}
