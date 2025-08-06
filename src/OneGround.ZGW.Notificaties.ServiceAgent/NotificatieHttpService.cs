using System;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Notificaties.Contracts.v1;

namespace OneGround.ZGW.Notificaties.ServiceAgent;

public class NotificatieHttpService : INotificatieService
{
    private readonly INotificatiesServiceAgent _notificatiesServiceAgent;

    public NotificatieHttpService(INotificatiesServiceAgent notificatiesServiceAgent)
    {
        _notificatiesServiceAgent = notificatiesServiceAgent;
    }

    public async Task NotifyAsync(Notification notificatie, CancellationToken ct = default)
    {
        var request = new NotificatieDto
        {
            Aanmaakdatum = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            Kanaal = notificatie.Kanaal,
            Actie = $"{notificatie.Actie}",
            HoofdObject = notificatie.HoodfObject,
            Resource = notificatie.Resource,
            ResourceUrl = notificatie.ResourceUrl,
            Kenmerken = notificatie.Kenmerken,
        };

        await _notificatiesServiceAgent.NotifyAsync(request, ct);
    }
}
