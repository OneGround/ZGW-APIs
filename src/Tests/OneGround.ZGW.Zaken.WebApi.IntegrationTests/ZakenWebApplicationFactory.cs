using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using OneGround.ZGW.Zaken.WebApi.BackgroundServices;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Boots the Zaken API in-process, with the RabbitMQ bus on the in-memory transport, notifications dropped and the BSN backfill service not started.
/// </summary>
internal sealed class ZakenWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public ZakenWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers) { }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());

        // The real service sets a RabbitMQ-only message priority, which throws on the in-memory transport
        services.Replace(ServiceDescriptor.Singleton<INotificatieService, NoOpNotificatieService>());

        RemoveHostedService<InpBsnBackfillService>(services);
    }

    private sealed class NoOpNotificatieService : INotificatieService
    {
        public Task NotifyAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
