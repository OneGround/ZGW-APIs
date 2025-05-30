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

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._2;

class DeleteZaakEigenschapCommandHandler
    : ZakenBaseHandler<DeleteZaakEigenschapCommandHandler>,
        IRequestHandler<DeleteZaakEigenschapCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public DeleteZaakEigenschapCommandHandler(
        ILogger<DeleteZaakEigenschapCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteZaakEigenschapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakEigenschap {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakEigenschap>();

        var zaakEigenschap = await _context
            .ZaakEigenschappen.Where(rsinFilter)
            .Include(b => b.Zaak)
            .SingleOrDefaultAsync(z => z.ZaakId == request.ZaakId && z.Id == request.Id, cancellationToken);

        if (zaakEigenschap == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakEigenschap.Zaak))
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakEigenschap.Zaak, errors))
        {
            return new CommandResult<KlantContact>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakEigenschap {Id}....", zaakEigenschap.Id);

            audittrail.SetOld<ZaakEigenschapResponseDto>(zaakEigenschap);

            _context.ZaakEigenschappen.Remove(zaakEigenschap);

            await audittrail.DestroyedAsync(zaakEigenschap.Zaak, zaakEigenschap, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakEigenschap {Id} successfully deleted.", zaakEigenschap.Id);
        }

        await SendNotificationAsync(Actie.destroy, zaakEigenschap, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakeigenschap" };
}

class DeleteZaakEigenschapCommand : IRequest<CommandResult>
{
    public Guid ZaakId { get; internal set; }
    public Guid Id { get; internal set; }
}
