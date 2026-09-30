using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.IntegrationTests.Common.Http;

/// <summary>
/// Stands in for the network behind every <see cref="HttpClient"/> the API creates through <see cref="IHttpClientFactory"/>
/// (the ZGW service agents, the token client). Requests are recorded and answered by <see cref="Responder"/>; with no
/// responder set, a request fails with an exception naming its URI, so an unexpected outbound call is visible in the test.
/// </summary>
public sealed class StubOutboundHttp
{
    private readonly ConcurrentQueue<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Answers an outbound request; <c>null</c> (the default) fails every request.
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; }

    /// <summary>
    /// Every outbound request the API made, in order.
    /// </summary>
    public HttpRequestMessage[] Requests => _requests.ToArray();

    internal HttpResponseMessage Send(HttpRequestMessage request)
    {
        _requests.Enqueue(request);

        var responder = Responder;
        if (responder == null)
        {
            throw new InvalidOperationException(
                $"Outbound HTTP is stubbed in integration tests and no response is set up for {request.Method} {request.RequestUri}."
            );
        }

        return responder(request);
    }
}

public static class StubOutboundHttpServiceCollectionExtensions
{
    /// <summary>
    /// Makes <paramref name="stub"/> the primary handler of every <see cref="HttpClient"/> created through <see cref="IHttpClientFactory"/>,
    /// so no request leaves the test process. The clients' own handlers (correlation id, authentication, resilience) still run.
    /// </summary>
    public static IServiceCollection AddStubOutboundHttp(this IServiceCollection services, StubOutboundHttp stub)
    {
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => new StubOutboundHttpMessageHandler(stub)));

        return services;
    }

    private sealed class StubOutboundHttpMessageHandler : HttpMessageHandler
    {
        private readonly StubOutboundHttp _stub;

        public StubOutboundHttpMessageHandler(StubOutboundHttp stub)
        {
            _stub = stub;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_stub.Send(request));
        }
    }
}
