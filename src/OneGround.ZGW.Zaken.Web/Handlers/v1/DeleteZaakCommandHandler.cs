using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.ServiceAgent.Extensions;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.ServiceAgent.v1;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class DeleteZaakCommandHandler : ZakenBaseHandler<DeleteZaakCommandHandler>, IRequestHandler<DeleteZaakCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IZakenServiceAgent _zakenServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteZaakCommandHandler(
        ILogger<DeleteZaakCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IZakenServiceAgent zakenServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _zakenServiceAgent = zakenServiceAgent;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteZaakCommand request, CancellationToken cancellationToken)
    {
        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            var zakenAndDeelZaken = await GetAllZakenAndDeelZakenAsync(request.Id, cancellationToken);
            if (!zakenAndDeelZaken.Any())
            {
                return new CommandResult(CommandStatus.NotFound);
            }

            // Synchroniseren relaties met informatieobjecten (zrc-005)
            foreach (var zaak in zakenAndDeelZaken)
            {
                await DeleteAndSyncZaakInformatieObjectenAsync(zaak, cancellationToken);
            }

            foreach (var zaak in zakenAndDeelZaken)
            {
                DeleteZaak(zaak); // Including audittrail with all details!

                // Audittrail: Record that only the zaak has been deleted WITHOUT any details expect the zaak identification (field toelichting)!!
                await audittrail.DestroyedAsync(zaak, toelichting: $"Betreft zaak met identificatie {zaak.Identificatie}", cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken); // Note: All zaak with eventually deel-zaken are removed within one commit!

            // Note: After successfull commit
            foreach (var zaak in zakenAndDeelZaken)
            {
                await SendNotificationAsync(Actie.destroy, zaak, cancellationToken);
            }
        }
        return new CommandResult(CommandStatus.OK);
    }

    private async Task<IList<Zaak>> GetAllZakenAndDeelZakenAsync(Guid zaakId, CancellationToken cancellationToken)
    {
        var zakenAndDeelZaken = new List<Zaak>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.AsSplitQuery()
            .Where(rsinFilter)
            .Include(z => z.Deelzaken)
            .Include(z => z.ZaakInformatieObjecten)
            .SingleOrDefaultAsync(z => z.Id == zaakId, cancellationToken);

        if (zaak != null)
        {
            foreach (var deelzaak in zaak.Deelzaken)
            {
                var deelzaakWithZaakInformatieObjecten = await _context
                    .Zaken.Where(rsinFilter)
                    .Include(z => z.ZaakInformatieObjecten)
                    .SingleOrDefaultAsync(z => z.Id == deelzaak.Id, cancellationToken);

                zakenAndDeelZaken.Add(deelzaakWithZaakInformatieObjecten);
            }

            zakenAndDeelZaken.Add(zaak);
        }

        return zakenAndDeelZaken;
    }

    private CommandResult DeleteZaak(Zaak zaak)
    {
        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        RemoveZaak(zaak);
        RemoveAudit(zaak);

        return new CommandResult(CommandStatus.OK);
    }

    private async Task DeleteAndSyncZaakInformatieObjectenAsync(Zaak zaak, CancellationToken cancellationToken)
    {
        // Note: Before deleting the zaak, delete all related zaakinformatieobjecten from ZRC via the ZakenServiceAgent!
        //   Doing so triggers the synchronization with DRC (the DocumentListener notificatie-receiver deletes the mirrored relation-ships)
        foreach (var zaakInformatieObject in zaak.ZaakInformatieObjecten)
        {
            _logger.LogDebug("Deleting and synchronizing ZaakInformatieObject {Id}....", zaakInformatieObject.Id);

            var result = await _zakenServiceAgent.DeleteZaakInformatieObjectByIdAsync(zaakInformatieObject.Id);
            if (!result.Success)
            {
                var errors = result.GetErrorsFromResponse();

                _logger.LogError("Failed to delete zaakinformatieobject {zaakInformatieObjectUrl}. {errors}", zaakInformatieObject.Url, errors);
            }
        }
    }

    private void RemoveZaak(Zaak zaak)
    {
        _logger.LogDebug("Deleting Zaak {Id} ...", zaak.Id);

        _context.Zaken.Remove(zaak);
    }

    private void RemoveAudit(Zaak zaak)
    {
        _logger.LogDebug("Deleting audit for Zaak {Id} ...", zaak.Id);

        var zaakAuditTrails = _context.AuditTrailRegels.Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == zaak.Id);

        _context.AuditTrailRegels.RemoveRange(zaakAuditTrails);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" };
}

class DeleteZaakCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
