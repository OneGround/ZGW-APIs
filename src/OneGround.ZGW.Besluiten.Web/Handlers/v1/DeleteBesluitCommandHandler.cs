using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Common.Batching;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.CorrelationId;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Messaging;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Messaging.Contracts;
using OneGround.ZGW.Zaken.ServiceAgent.v1;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class DeleteBesluitCommandHandler : BesluitenBaseHandler<DeleteBesluitCommandHandler>, IRequestHandler<DeleteBesluitCommand, CommandResult>
{
    private readonly BrcDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public DeleteBesluitCommandHandler(
        ILogger<DeleteBesluitCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        INotificatieService notificatieService,
        IPublishEndpoint publishEndpoint,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IZakenServiceAgent zakenServiceAgent,
        ICorrelationContextAccessor correlationContextAccessor,
        IBatchIdAccessor batchIdAccessor,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _auditTrailFactory = auditTrailFactory;
        _zakenServiceAgent = zakenServiceAgent;
        _correlationContextAccessor = correlationContextAccessor;
        _batchIdAccessor = batchIdAccessor;
    }

    public async Task<CommandResult> Handle(DeleteBesluitCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Besluit {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context
            .Besluiten.Where(rsinFilter)
            .Include(z => z.BesluitInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluit == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(besluit))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            // Note: This also implies: Vernietigen van besluiten (brc-008)
            foreach (var besluitInformatieObject in besluit.BesluitInformatieObjecten)
            {
                _logger.LogDebug("Synchronizing BesluitInformatieObject {Id}....", besluitInformatieObject.Id);

                byte priority = (byte)(string.IsNullOrEmpty(_batchIdAccessor.Id) ? MessagePriority.Normal : MessagePriority.Low);

                // Note: During deletion, the mirrored relationship is also deleted from the Documents API.

                // Synchroniseren relaties met informatieobjecten (brc-005)
                await _publishEndpoint.Publish<IDeleteObjectInformatieObject>(
                    new
                    {
                        Object = _uriService.GetUri(besluitInformatieObject.Besluit),
                        ObjectDestroy = true,
                        besluitInformatieObject.InformatieObject,
                        Rsin = _rsin,
                        _correlationContextAccessor.CorrelationId,
                    },
                    context => context.SetPriority(priority),
                    cancellationToken
                );

                _context.BesluitInformatieObjecten.Remove(besluitInformatieObject);
            }

            _logger.LogDebug("Deleting Besluit {Id}....", besluit.Id);

            // Remove the audittrail for this besluit first (this contains BesluitInformatieObjecten also)
            var audittrailregels = _context.AuditTrailRegels.Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == besluit.Id);

            _context.AuditTrailRegels.RemoveRange(audittrailregels);

            _context.Besluiten.Remove(besluit);

            await audittrail.DestroyedAsync(besluit, toelichting: $"Betreft besluit met identificatie {besluit.Identificatie}", cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Besluit {Id} successfully deleted.", besluit.Id);
        }

        if (besluit.ZaakBesluitUrl != null)
        {
            await _zakenServiceAgent.DeleteZaakBesluitByUrlAsync(besluit.ZaakBesluitUrl);
        }

        await SendNotificationAsync(Actie.destroy, besluit, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" };
}

class DeleteBesluitCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
