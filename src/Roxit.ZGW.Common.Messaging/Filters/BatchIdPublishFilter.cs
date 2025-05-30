using System.Linq;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Roxit.ZGW.Common.Batching;
using Headers = Roxit.ZGW.Common.Constants.Headers;

namespace Roxit.ZGW.Common.Messaging.Filters;

public class BatchIdPublishFilter<T> : IFilter<PublishContext<T>>
    where T : class
{
    private readonly IBatchIdAccessor _batchIdAccessor;

    public BatchIdPublishFilter(IBatchIdAccessor batchIdAccessor)
    {
        _batchIdAccessor = batchIdAccessor;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SendBatchIdHeader");
    }

    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        if (
            !string.IsNullOrEmpty(_batchIdAccessor.Id)
            && !context.Headers.Any(h => h.Key.Equals(Headers.BatchId, System.StringComparison.OrdinalIgnoreCase))
        )
        {
            context.Headers.Set(Headers.BatchId, _batchIdAccessor.Id);
        }

        return next.Send(context);
    }
}
