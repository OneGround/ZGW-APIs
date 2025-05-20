using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._2;

class DeleteZaakObjectCommandHandler : ZakenBaseHandler<DeleteZaakObjectCommandHandler>, IRequestHandler<DeleteZaakObjectCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteZaakObjectCommandHandler(
        ILogger<DeleteZaakObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteZaakObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakObject {ZaakObjectId}....", request.ZaakObjectId);

        var rsinFilter = GetRsinFilterPredicate<ZaakObject>();

        var zaakObject = await _context
            .ZaakObjecten.Where(rsinFilter)
            .Include(b => b.Zaak)
            //.ThenInclude(r => r.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.ZaakObjectId, cancellationToken);

        if (zaakObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakObject.Zaak))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakObject {zaakObjectId}....", zaakObject.Id);

            audittrail.SetOld<ZaakObjectResponseDto>(zaakObject);

            _context.ZaakObjecten.Remove(zaakObject);

            await audittrail.DestroyedAsync(zaakObject.Zaak, zaakObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakObject {zaakObjectId} successfully deleted.", zaakObject.Id);
        }

        await SendNotificationAsync(Actie.destroy, zaakObject, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" };
}

class DeleteZaakObjectCommand : IRequest<CommandResult>
{
    public Guid ZaakObjectId { get; set; }
}
