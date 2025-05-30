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
using Roxit.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakRol;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._5;

class DeleteZaakRolCommandHandler : ZakenBaseHandler<DeleteZaakRolCommandHandler>, IRequestHandler<DeleteZaakRolCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public DeleteZaakRolCommandHandler(
        ILogger<DeleteZaakRolCommandHandler> logger,
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

    public async Task<CommandResult> Handle(DeleteZaakRolCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Delete ZaakRol {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakRol>();

        var zaakRol = await _context
            .ZaakRollen.Where(rsinFilter)
            .Include(r => r.Zaak)
            .ThenInclude(r => r.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakRol == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakRol.Zaak))
        {
            return new CommandResult<ZaakRol>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakRol.Zaak, errors))
        {
            return new CommandResult<KlantContact>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ZaakRol {Id}....", zaakRol.Id);

            audittrail.SetOld<ZaakRolResponseDto>(zaakRol);

            _context.ZaakRollen.Remove(zaakRol);

            await audittrail.DestroyedAsync(zaakRol.Zaak, zaakRol, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakRol {Id} successfully deleted.", zaakRol.Id);
        }

        await SendNotificationAsync(Actie.destroy, zaakRol, cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "rol" };
}

class DeleteZaakRolCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
