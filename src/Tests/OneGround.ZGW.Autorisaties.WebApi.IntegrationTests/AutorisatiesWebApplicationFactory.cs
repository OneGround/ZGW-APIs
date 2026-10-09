using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;

namespace OneGround.ZGW.Autorisaties.WebApi.IntegrationTests;

/// <summary>
/// Boots the Autorisaties API in-process, with the RabbitMQ bus on the in-memory transport and notifications dropped; its own database authorization resolver stays in place.
/// </summary>
internal sealed class AutorisatiesWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public AutorisatiesWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers) { }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());

        // The real service sets a RabbitMQ-only message priority, which throws on the in-memory transport
        services.Replace(ServiceDescriptor.Singleton<INotificatieService, NoOpNotificatieService>());
    }

    private sealed class NoOpNotificatieService : INotificatieService
    {
        public Task NotifyAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
