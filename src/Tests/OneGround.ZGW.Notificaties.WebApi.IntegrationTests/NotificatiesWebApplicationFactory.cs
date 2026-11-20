using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.IntegrationTests.Common;
using OneGround.ZGW.IntegrationTests.Common.Containers;

namespace OneGround.ZGW.Notificaties.WebApi.IntegrationTests;

/// <summary>
/// Boots the Notificaties API in-process, with the RabbitMQ bus on the in-memory transport and the queued notifications captured instead of sent;
/// the API's own authorization resolver stays in place and answers from the stub Autorisaties API.
/// </summary>
internal sealed class NotificatiesWebApplicationFactory : ZgwWebApplicationFactory<Program>
{
    public NotificatiesWebApplicationFactory(IntegrationTestContainers containers)
        : base(containers) { }

    /// <summary>
    /// The notifications the API queued for sending.
    /// </summary>
    public ConcurrentQueue<Notification> QueuedNotifications { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddMassTransitTestHarness(x => x.AddRequestClient<ISendNotificaties>());

        // The real service sets a RabbitMQ-only message priority, which throws on the in-memory transport
        services.Replace(ServiceDescriptor.Singleton<INotificatieService>(new CapturingNotificatieService(QueuedNotifications)));
    }

    private sealed class CapturingNotificatieService(ConcurrentQueue<Notification> queue) : INotificatieService
    {
        public Task NotifyAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            queue.Enqueue(notification);
            return Task.CompletedTask;
        }
    }
}
