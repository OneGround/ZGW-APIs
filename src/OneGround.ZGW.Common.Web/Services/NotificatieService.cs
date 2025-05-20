using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Messaging;

namespace OneGround.ZGW.Common.Web.Services;

public class Notification
{
    public string Kanaal { get; set; }
    public string HoodfObject { get; set; }
    public string Resource { get; set; }
    public string ResourceUrl { get; set; }
    public string Actie { get; set; }
    public IDictionary<string, string> Kenmerken { get; set; }
    public string Rsin { get; set; }
    public bool Ignore { get; set; }
}

public class NotificatieService : INotificatieService
{
    private readonly ILogger<NotificatieService> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public NotificatieService(
        ILogger<NotificatieService> logger,
        IPublishEndpoint publishEndpoint,
        ICorrelationContextAccessor correlationContextAccessor,
        IBatchIdAccessor batchIdAccessor
    )
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _correlationContextAccessor = correlationContextAccessor;
        _batchIdAccessor = batchIdAccessor;
    }

    public async Task NotifyAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Publishing {@Notification}", notification);

        byte priority = (byte)(string.IsNullOrEmpty(_batchIdAccessor.Id) ? MessagePriority.Normal : MessagePriority.Low);

        await _publishEndpoint.Publish<ISendNotificaties>(
            new
            {
                notification.Kanaal,
                HoofdObject = notification.HoodfObject,
                notification.Resource,
                notification.ResourceUrl,
                notification.Actie,
                notification.Kenmerken,
                notification.Rsin,
                notification.Ignore,
                _correlationContextAccessor.CorrelationId,
            },
            context => context.SetPriority(priority),
            cancellationToken: cancellationToken
        );
    }
}
