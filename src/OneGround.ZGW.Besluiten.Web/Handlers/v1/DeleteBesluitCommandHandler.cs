using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Besluiten.Web.Notificaties;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
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

    public DeleteBesluitCommandHandler(
        ILogger<DeleteBesluitCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IZakenServiceAgent zakenServiceAgent,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, besluitKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _zakenServiceAgent = zakenServiceAgent;
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
            RemoveBesluitInformatieObject(besluit);

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

        await SendInformationObjectNotificationAsync(besluit, cancellationToken);
        await SendNotificationAsync(Actie.destroy, besluit, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private void RemoveBesluitInformatieObject(Besluit besluit)
    {
        _logger.LogDebug("Deleting {BesluitInformatieObject} from besluit {Id}....", nameof(BesluitInformatieObject), besluit.Id);
        foreach (var besluitInformatieObject in besluit.BesluitInformatieObjecten)
        {
            _context.BesluitInformatieObjecten.Remove(besluitInformatieObject);
        }
    }

    private async Task SendInformationObjectNotificationAsync(Besluit besluit, CancellationToken cancellationToken)
    {
        const string besluitinformatieobjectKenmerken = "besluitinformatieobject.informatieobject";
        foreach (var besluitInformatieObject in besluit.BesluitInformatieObjecten)
        {
            var extraKenmerken = new Dictionary<string, string> { { besluitinformatieobjectKenmerken, besluitInformatieObject.InformatieObject } };

            await SendNotificationAsync(Actie.destroy, besluitInformatieObject, extraKenmerken, cancellationToken);
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" };
}

class DeleteBesluitCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
