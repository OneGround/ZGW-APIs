using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace OneGround.ZGW.Common.Messaging;

public abstract class ConsumerBase<TConsumer>
    where TConsumer : class
{
    protected readonly ILogger<TConsumer> Logger;

    protected ConsumerBase(ILogger<TConsumer> logger)
    {
        Logger = logger;
    }

    protected IDisposable GetLoggingScope<TMessage>(TMessage message, Guid correlationId)
        where TMessage : IRsinContract
    {
        return Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId, ["RSIN"] = message.Rsin });
    }
}
