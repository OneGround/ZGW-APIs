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
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._7;

class UpdateZaakNotitieCommandHandler
    : ZakenBaseHandler<UpdateZaakNotitieCommandHandler>,
        IRequestHandler<UpdateZaakNotitieCommand, CommandResult<ZaakNotitie>>
{
    private readonly ZrcDbContext _context;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateZaakNotitieCommandHandler(
        ILogger<UpdateZaakNotitieCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakNotitie>> Handle(UpdateZaakNotitieCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Update ZaakNotitie {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakNotitie>();

        var zaakNotitie = await _context
            .ZaakNotities.Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakNotitie == null)
        {
            return new CommandResult<ZaakNotitie>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakNotitie.Zaak))
        {
            return new CommandResult<ZaakNotitie>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakNotitie.Zaak, errors))
        {
            return new CommandResult<ZaakNotitie>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        // Note: A notitie can only be modified as long as it is a concept (checked on the stored status, so a definitief notitie cannot be reopened)
        if (zaakNotitie.Status != ZaakNotitieStatus.concept)
        {
            var error = new ValidationError("status", ErrorCode.Invalid, "Een notitie kan alleen gewijzigd worden zolang de status concept is.");
            return new CommandResult<ZaakNotitie>(null, CommandStatus.ValidationError, error);
        }

        // Note: A notitie cannot be moved to another zaak
        if (UriHelper.GetResourceId(request.ZaakUrl) != zaakNotitie.ZaakId)
        {
            var error = new ValidationError("gerelateerdAan", ErrorCode.Invalid, "De zaak van een notitie kan niet gewijzigd worden.");
            return new CommandResult<ZaakNotitie>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions, zaakNotitie.Zaak.LegacyAuditTrail))
        {
            audittrail.SetOld<ZaakNotitieResponseDto>(zaakNotitie);

            zaakNotitie.Onderwerp = request.ZaakNotitie.Onderwerp;
            zaakNotitie.Tekst = request.ZaakNotitie.Tekst;
            zaakNotitie.AangemaaktDoor = request.ZaakNotitie.AangemaaktDoor;

            // Note: Fields that are left out of a complete update keep their stored value
            if (!request.KeepNotitieType)
            {
                zaakNotitie.NotitieType = request.ZaakNotitie.NotitieType;
            }

            if (!request.KeepStatus)
            {
                zaakNotitie.Status = request.ZaakNotitie.Status;
            }

            audittrail.SetNew<ZaakNotitieResponseDto>(zaakNotitie);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakNotitie.Zaak, zaakNotitie, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakNotitie.Zaak, zaakNotitie, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakNotitie {Id} successfully updated.", zaakNotitie.Id);

        return new CommandResult<ZaakNotitie>(zaakNotitie, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaaknotitie" };
}

class UpdateZaakNotitieCommand : IRequest<CommandResult<ZaakNotitie>>
{
    public Guid Id { get; internal set; }
    public ZaakNotitie ZaakNotitie { get; internal set; }
    public string ZaakUrl { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
    public bool KeepNotitieType { get; internal set; }
    public bool KeepStatus { get; internal set; }
}
