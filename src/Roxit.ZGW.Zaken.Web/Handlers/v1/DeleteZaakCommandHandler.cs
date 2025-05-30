using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Batching;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.CorrelationId;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Messaging;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.Messaging.Contracts;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class DeleteZaakCommandHandler : ZakenBaseHandler<DeleteZaakCommandHandler>, IRequestHandler<DeleteZaakCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly IBatchIdAccessor _batchIdAccessor;

    public DeleteZaakCommandHandler(
        ILogger<DeleteZaakCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IPublishEndpoint publishEndpoint,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor,
        IBatchIdAccessor batchIdAccessor,
        INotificatieService notificatieService
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _auditTrailFactory = auditTrailFactory;
        _correlationContextAccessor = correlationContextAccessor;
        _batchIdAccessor = batchIdAccessor;
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
                await SynchronizeZaakInformatieObjectenAsync(zaak, cancellationToken);

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

        RemoveZaakInformatieObjecten(zaak);
        RemoveZaak(zaak);
        RemoveAudit(zaak);

        return new CommandResult(CommandStatus.OK);
    }

    private void RemoveZaakInformatieObjecten(Zaak zaak)
    {
        _logger.LogDebug("Deleting ZaakInformatieObjecten from zaak {Id}....", zaak.Id);

        var zaakInformatieObjecten = _context.ZaakInformatieObjecten.Where(a => a.ZaakId == zaak.Id);

        _context.ZaakInformatieObjecten.RemoveRange(zaakInformatieObjecten);
    }

    private async Task SynchronizeZaakInformatieObjectenAsync(Zaak zaak, CancellationToken cancellationToken)
    {
        foreach (var zaakInformatieObject in zaak.ZaakInformatieObjecten)
        {
            _logger.LogDebug("Synchronizing ZaakInformatieObject {Id}....", zaakInformatieObject.Id);

            byte priority = (byte)(string.IsNullOrEmpty(_batchIdAccessor.Id) ? MessagePriority.Normal : MessagePriority.Low);

            //
            // Note: During deletion, the mirrored relationship is also deleted from the Documents API.

            // Synchroniseren relaties met informatieobjecten (zrc-005)
            await _publishEndpoint.Publish<IDeleteObjectInformatieObject>(
                new
                {
                    Object = _uriService.GetUri(zaakInformatieObject.Zaak),
                    ObjectDestroy = true,
                    zaakInformatieObject.InformatieObject,
                    Rsin = _rsin,
                    _correlationContextAccessor.CorrelationId,
                },
                context => context.SetPriority(priority),
                cancellationToken
            );
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
