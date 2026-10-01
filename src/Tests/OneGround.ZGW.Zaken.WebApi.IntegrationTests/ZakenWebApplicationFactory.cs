using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;
using OneGround.ZGW.Zaken.WebApi.BackgroundServices;

namespace OneGround.ZGW.Zaken.WebApi.IntegrationTests;

/// <summary>
/// Boots the Zaken API in-process, with the RabbitMQ bus on the in-memory transport and the BSN backfill service not started.
/// </summary>
public sealed class ZakenWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public ZakenWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers) { }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());

        RemoveHostedService<InpBsnBackfillService>(services);
    }
}
