using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class DeleteZaakBesluitCommandHandler : ZakenBaseHandler<DeleteZaakBesluitCommandHandler>, IRequestHandler<DeleteZaakBesluitCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteZaakBesluitCommandHandler(
        ILogger<DeleteZaakBesluitCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteZaakBesluitCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakBesluit {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakBesluit>(z => z.Zaak.Owner == _rsin);

        var besluit = await _context
            .ZaakBesluiten.Where(rsinFilter)
            .Include(b => b.Zaak)
            .ThenInclude(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.ZaakId == request.ZaakId && z.Id == request.Id, cancellationToken);

        if (besluit == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(besluit.Zaak))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakBesluit {Id}....", besluit.Id);

            audittrail.SetOld<ZaakBesluitResponseDto>(besluit);

            _context.ZaakBesluiten.Remove(besluit);

            await audittrail.DestroyedAsync(besluit.Zaak, besluit, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakBesluit {Id} successfully deleted.", besluit.Id);
        }

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakbesluit" };
}

class DeleteZaakBesluitCommand : IRequest<CommandResult>
{
    public Guid ZaakId { get; set; }
    public Guid Id { get; set; }
}
