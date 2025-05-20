using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Web.Handlers;

class QueueNotificatieCommandHandler : IRequestHandler<QueueNotificatieCommand, CommandResult<Notificatie>>
{
    private readonly INotificatieService _notificatieService;
    private readonly IAuthorizationContextAccessor _authorizationContextAccessor;
    private readonly ILogger<QueueNotificatieCommandHandler> _logger;

    public QueueNotificatieCommandHandler(
        INotificatieService notificatieService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ILogger<QueueNotificatieCommandHandler> logger
    )
    {
        _notificatieService = notificatieService;
        _authorizationContextAccessor = authorizationContextAccessor;
        _logger = logger;
    }

    public async Task<CommandResult<Notificatie>> Handle(QueueNotificatieCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Notificatie {@Notification} received. ", request.Notificatie);

        var notification = new Notification
        {
            Kanaal = request.Notificatie.Kanaal,
            HoodfObject = request.Notificatie.HoofdObject,
            Resource = request.Notificatie.Resource,
            ResourceUrl = request.Notificatie.ResourceUrl,
            Actie = request.Notificatie.Actie,
            Kenmerken = request.Notificatie.Kenmerken,
            Rsin = _authorizationContextAccessor.AuthorizationContext.Authorization.Rsin,
        };
        await _notificatieService.NotifyAsync(notification, cancellationToken);

        return new CommandResult<Notificatie>(request.Notificatie, CommandStatus.OK);
    }
}

class QueueNotificatieCommand : IRequest<CommandResult<Notificatie>>
{
    public Notificatie Notificatie { get; set; }
}
