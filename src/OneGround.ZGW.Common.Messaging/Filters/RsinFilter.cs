using System.Threading.Tasks;
using GreenPipes;
using MassTransit;
using OneGround.ZGW.Common.Authentication;

namespace OneGround.ZGW.Common.Messaging.Filters;

public class RsinFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IOrganisationContextFactory _organisationContextFactory;

    public RsinFilter(IOrganisationContextFactory organisationContextFactory)
    {
        _organisationContextFactory = organisationContextFactory;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("Rsin");
    }

    public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        if (context.Message is IRsinContract message)
        {
            _organisationContextFactory.Create(message.Rsin);
        }

        return next.Send(context);
    }
}
