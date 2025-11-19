using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Roxit.ZGW.Documenten.Jobs.Subscription;

namespace OneGround.ZGW.Documenten.Messaging.Listener.Services;

public class ManageSubscriptionsHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        BackgroundJob.Enqueue<ManageSubscriptionsJob>(job => job.ExecuteAsync());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
