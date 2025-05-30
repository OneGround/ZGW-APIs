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
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class DeleteZaakResultaatCommandHandler
    : ZakenBaseHandler<DeleteZaakResultaatCommandHandler>,
        IRequestHandler<DeleteZaakResultaatCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public DeleteZaakResultaatCommandHandler(
        ILogger<DeleteZaakResultaatCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteZaakResultaatCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakResultaat {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakResultaat>();

        var zaakResultaat = await _context
            .ZaakResultaten.Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakResultaat == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakResultaat.Zaak))
        {
            return new CommandResult<ZaakResultaat>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakResultaat.Zaak, errors))
        {
            return new CommandResult<KlantContact>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakResultaat {Id}....", zaakResultaat.Id);

            audittrail.SetOld<ZaakResultaatResponseDto>(zaakResultaat);

            _context.ZaakResultaten.Remove(zaakResultaat);

            await audittrail.DestroyedAsync(zaakResultaat.Zaak, zaakResultaat, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakResultaat {Id} successfully deleted.", zaakResultaat.Id);
        }

        await SendNotificationAsync(Actie.destroy, zaakResultaat, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "resultaat" };
}

class DeleteZaakResultaatCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
