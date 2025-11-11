using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.ServiceAgent.v1;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.ServiceAgent.v1;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class DeleteBesluitCommandHandler : BesluitenBaseHandler<DeleteBesluitCommandHandler>, IRequestHandler<DeleteBesluitCommand, CommandResult>
{
    private readonly BrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly IBesluitenServiceAgent _besluitenServiceAgent;

    public DeleteBesluitCommandHandler(
        ILogger<DeleteBesluitCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IZakenServiceAgent zakenServiceAgent,
        IBesluitenServiceAgent besluitenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, besluitKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _zakenServiceAgent = zakenServiceAgent;
        _besluitenServiceAgent = besluitenServiceAgent;
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
            await DeleteAndSyncBesluitInformatieObjectenAsync(besluit, cancellationToken);

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

    private async Task DeleteAndSyncBesluitInformatieObjectenAsync(Besluit besluit, CancellationToken cancellationToken)
    {
        // Note: Before deleting the besluit, delete all related besluitinformatieobjecten from BRC via the BesluitenServiceAgent!
        //   Doing so triggers the synchronization with DRC (the DocumentListener notificatie-receiver deletes the mirrored relation-ships)
        foreach (var besluitInformatieObject in besluit.BesluitInformatieObjecten)
        {
            _logger.LogDebug("Deleting and synchronizing BesluitInformatieObject {Id}....", besluitInformatieObject.Id);

            var result = await _besluitenServiceAgent.DeleteBesluitInformatieObjectByIdAsync(besluitInformatieObject.Id);
            if (!result.Success)
            {
                var errors = result.GetErrorsFromResponse();
                _logger.LogError(
                    "Failed to delete besluitinformatieobject {besluitInformatieObjectUrl}. {errors}",
                    besluitInformatieObject.Url,
                    errors
                );
            }
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" };
}

class DeleteBesluitCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
