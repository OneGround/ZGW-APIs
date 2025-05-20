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
using OneGround.ZGW.Common.Web;
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

class UpdateZaakObjectCommandHandler
    : ZakenBaseHandler<UpdateZaakObjectCommandHandler>,
        IRequestHandler<UpdateZaakObjectCommand, CommandResult<ZaakObject>>
{
    private readonly ZrcDbContext _context;
    private readonly IEntityUpdater<ZaakObject> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateZaakObjectCommandHandler(
        ILogger<UpdateZaakObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUpdater<ZaakObject> entityUpdater,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakObject>> Handle(UpdateZaakObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakObject {ZaakObjectId}....", request.ZaakObjectId);

        var rsinFilter = GetRsinFilterPredicate<ZaakObject>();

        var zaakobject = await _context
            .ZaakObjecten.Where(rsinFilter)
            .Include(b => b.Zaak)
            .Include(z => z.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Include(z => z.Adres)
            .Include(z => z.Buurt)
            .Include(z => z.Pand)
            .Include(z => z.KadastraleOnroerendeZaak)
            .Include(z => z.Gemeente)
            .Include(z => z.TerreinGebouwdObject)
            .Include(z => z.Overige)
            .Include(z => z.WozWaardeObject.IsVoor.AanduidingWozObject)
            //.ThenInclude(r => r.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.ZaakObjectId, cancellationToken);

        if (zaakobject == null)
        {
            return new CommandResult<ZaakObject>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakobject.Zaak))
        {
            return new CommandResult<ZaakObject>(null, CommandStatus.Forbidden);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Updating ZaakObject {zaakobjectId}....", zaakobject.Id);

            audittrail.SetOld<ZaakObjectResponseDto>(zaakobject);

            _entityUpdater.Update(request.ZaakObject, zaakobject);

            audittrail.SetNew<ZaakObjectResponseDto>(zaakobject);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakobject.Zaak, zaakobject, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakobject.Zaak, zaakobject, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakObject {zaakobjectId} successfully updated.", zaakobject.Id);

        await SendNotificationAsync(Actie.update, zaakobject, cancellationToken);

        return new CommandResult<ZaakObject>(zaakobject, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" };
}

class UpdateZaakObjectCommand : IRequest<CommandResult<ZaakObject>>
{
    public Guid ZaakObjectId { get; internal set; }
    public ZaakObject ZaakObject { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
