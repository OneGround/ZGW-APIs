using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Roxit.ZGW.Common.Constants;

namespace Roxit.ZGW.Common.CorrelationId;

public class CorrelationIdHandler : DelegatingHandler
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CorrelationIdHandler(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _correlationContextAccessor.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        if (!request.Headers.Contains(Headers.CorrelationHeader))
        {
            request.Headers.Add(Headers.CorrelationHeader, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
