using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using OneGround.ZGW.Common.CorrelationId;

namespace OneGround.ZGW.Common.Messaging.Filters;

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
