using System.Collections.Generic;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Batching;
using Headers = OneGround.ZGW.Common.Constants.Headers;

namespace OneGround.ZGW.Common.Messaging.Filters;

public class BatchIdConsumingFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IBatchIdAccessor _batchIdAccessor;
    private readonly ILogger<BatchIdConsumingFilter<T>> _logger;

    public BatchIdConsumingFilter(IBatchIdAccessor batchIdAccessor, ILogger<BatchIdConsumingFilter<T>> logger)
    {
        _batchIdAccessor = batchIdAccessor;
        _logger = logger;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("ReadBatchIdHeader");
    }

    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (!context.Headers.TryGetHeader(Headers.BatchId, out var batchId))
            return next.Send(context);

        using (_logger.BeginScope(new Dictionary<string, object> { ["BatchId"] = batchId }))
        {
            _batchIdAccessor.Id = $"{batchId}";
            return next.Send(context);
        }
    }
}
