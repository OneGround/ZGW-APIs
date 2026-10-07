using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Boots the Autorisaties API in-process, with the RabbitMQ bus on the in-memory transport; its own database authorization resolver stays in place.
/// </summary>
internal sealed class AutorisatiesWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public AutorisatiesWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers) { }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());
    }
}
