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
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._7;

class DeleteZaakNotitieCommandHandler : ZakenBaseHandler<DeleteZaakNotitieCommandHandler>, IRequestHandler<DeleteZaakNotitieCommand, CommandResult>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public DeleteZaakNotitieCommandHandler(
        ILogger<DeleteZaakNotitieCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult> Handle(DeleteZaakNotitieCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Delete ZaakNotitie {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakNotitie>();

        var zaakNotitie = await _context
            .ZaakNotities.Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakNotitie == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakNotitie.Zaak))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakNotitie.Zaak, errors))
        {
            return new CommandResult(CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions, zaakNotitie.Zaak.LegacyAuditTrail))
        {
            _logger.LogDebug("Deleting ZaakNotitie {Id}....", zaakNotitie.Id);

            audittrail.SetOld<ZaakNotitieResponseDto>(zaakNotitie);

            _context.ZaakNotities.Remove(zaakNotitie);

            await audittrail.DestroyedAsync(zaakNotitie.Zaak, zaakNotitie, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakNotitie {Id} successfully deleted.", zaakNotitie.Id);
        }

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaaknotitie" };
}

class DeleteZaakNotitieCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
