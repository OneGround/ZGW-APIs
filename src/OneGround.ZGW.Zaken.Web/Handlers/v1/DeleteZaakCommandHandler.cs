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
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class DeleteZaakCommandHandler : ZakenBaseHandler<DeleteZaakCommandHandler>, IRequestHandler<DeleteZaakCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteZaakCommandHandler(
        ILogger<DeleteZaakCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
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
                await SendInformationObjectNotificationAsync(zaak, cancellationToken);
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

    private async Task SendInformationObjectNotificationAsync(Zaak zaak, CancellationToken cancellationToken)
    {
        const string zaakInformatieObjectKenmerken = "zaakinformatieobject.informatieobject";
        foreach (var zaakInformatieObject in zaak.ZaakInformatieObjecten)
        {
            var extraKenmerken = new Dictionary<string, string> { { zaakInformatieObjectKenmerken, zaakInformatieObject.InformatieObject } };
            await SendNotificationAsync(Actie.destroy, zaakInformatieObject, extraKenmerken, cancellationToken);
        }
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" };
}

class DeleteZaakCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
