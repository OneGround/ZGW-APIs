using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace OneGround.ZGW.IntegrationTests.Common.Http;

/// <summary>
/// Stands in for the network behind every <see cref="HttpClient"/> the API creates; an unanswered request throws, naming its URI.
/// </summary>
public sealed class StubOutboundHttp
{
    private readonly ConcurrentQueue<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Answers an outbound request; returning <c>null</c> fails the request.
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; }

    /// <summary>
    /// Every outbound request the API made, in order.
    /// </summary>
    public HttpRequestMessage[] Requests => _requests.ToArray();

    internal HttpResponseMessage Send(HttpRequestMessage request)
    {
        _requests.Enqueue(request);

        return Responder?.Invoke(request)
            ?? throw new InvalidOperationException(
                $"Outbound HTTP is stubbed in integration tests and no response is set up for {request.Method} {request.RequestUri}."
            );
    }
}

public static class StubOutboundHttpServiceCollectionExtensions
{
    /// <summary>
    /// Makes <paramref name="stub"/> the primary handler of every factory-created <see cref="HttpClient"/>, keeping the clients' own handlers.
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
