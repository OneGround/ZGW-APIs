using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.Common.CorrelationId;

public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private readonly ILogger<CorrelationContextAccessor> _logger;
    private static readonly AsyncLocal<CorrelationContext> CorrelationIdContext = new();
    public string CorrelationId => CorrelationIdContext.Value?.CorrelationId;

    public CorrelationContextAccessor(ILogger<CorrelationContextAccessor> logger)
    {
        _logger = logger;
    }

    public IDisposable SetCorrelationId(string correlationId)
    {
        var correlationContext = new CorrelationContext(correlationId);
        CorrelationIdContext.Value = correlationContext;

        var keyValuePairs = new[] { new KeyValuePair<string, object>("CorrelationId", correlationId) };
        return _logger.BeginScope(keyValuePairs);
    }

    private sealed class CorrelationContext
    {
        public string CorrelationId { get; }

        public CorrelationContext(string correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
