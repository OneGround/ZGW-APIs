using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using Roxit.ZGW.Common.CorrelationId;

namespace Roxit.ZGW.Common.Messaging.Filters;

public class CorrelationIdFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CorrelationIdFilter(ICorrelationContextAccessor correlationContextAccessor)
    {
        _correlationContextAccessor = correlationContextAccessor;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("CorrelationId");
    }

    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        _correlationContextAccessor.SetCorrelationId(context.CorrelationId.ToString());

        return next.Send(context);
    }
}
