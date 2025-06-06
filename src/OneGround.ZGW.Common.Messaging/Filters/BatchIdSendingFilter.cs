using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using OneGround.ZGW.Common.Batching;
using Headers = OneGround.ZGW.Common.Constants.Headers;

namespace OneGround.ZGW.Common.Messaging.Filters;

public class BatchIdSendingFilter<T> : IFilter<SendContext<T>>
    where T : class
{
    private readonly IBatchIdAccessor _batchIdAccessor;

    public BatchIdSendingFilter(IBatchIdAccessor batchIdAccessor)
    {
        _batchIdAccessor = batchIdAccessor;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("SendBatchIdHeader");
    }

    public Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
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
