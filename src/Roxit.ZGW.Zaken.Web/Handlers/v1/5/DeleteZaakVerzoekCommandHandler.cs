using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._5;

class DeleteZaakVerzoekCommandHandler : ZakenBaseHandler<DeleteZaakVerzoekCommandHandler>, IRequestHandler<DeleteZaakVerzoekCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public DeleteZaakVerzoekCommandHandler(
        ILogger<DeleteZaakVerzoekCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult> Handle(DeleteZaakVerzoekCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Delete ZaakVerzoek {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakVerzoek>();

        var zaakVerzoek = await _context
            .ZaakVerzoeken.Where(rsinFilter)
            .Include(r => r.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakVerzoek == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakVerzoek.Zaak))
        {
            return new CommandResult<ZaakVerzoek>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakVerzoek.Zaak, errors))
        {
            return new CommandResult<KlantContact>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakVerzoek {Id}....", zaakVerzoek.Id);

            audittrail.SetOld<ZaakVerzoekResponseDto>(zaakVerzoek);

            _context.ZaakVerzoeken.Remove(zaakVerzoek);

            await audittrail.DestroyedAsync(zaakVerzoek.Zaak, zaakVerzoek, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakVerzoek {Id} successfully deleted.", zaakVerzoek.Id);
        }

        await SendNotificationAsync(Actie.destroy, zaakVerzoek, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakverzoek" };
}

class DeleteZaakVerzoekCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
