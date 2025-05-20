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
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class UpdateZaakResultaatCommandHandler
    : ZakenBaseHandler<UpdateZaakResultaatCommandHandler>,
        IRequestHandler<UpdateZaakResultaatCommand, CommandResult<ZaakResultaat>>
{
    private readonly ZrcDbContext _context;
    private readonly IEntityUpdater<ZaakResultaat> _entityUpdater;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateZaakResultaatCommandHandler(
        ILogger<UpdateZaakResultaatCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IEntityUpdater<ZaakResultaat> entityUpdater,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult<ZaakResultaat>> Handle(UpdateZaakResultaatCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakResultaat {Id} and validating....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakResultaat>();

        var zaakResultaat = await _context
            .ZaakResultaten.Where(rsinFilter)
            .Include(z => z.Zaak)
            .ThenInclude(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakResultaat == null)
        {
            return new CommandResult<ZaakResultaat>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakResultaat.Zaak))
        {
            return new CommandResult<ZaakResultaat>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakResultaat.Zaak, errors))
        {
            return new CommandResult<ZaakResultaat>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Updating ZaakResultaat {Id}....", zaakResultaat.Id);

            audittrail.SetOld<ZaakResultaatResponseDto>(zaakResultaat);

            _entityUpdater.Update(request.ZaakResultaat, zaakResultaat);

            audittrail.SetNew<ZaakResultaatResponseDto>(zaakResultaat);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakResultaat.Zaak, zaakResultaat, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakResultaat.Zaak, zaakResultaat, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakResultaat {Id} successfully updated.", zaakResultaat.Id);
        }

        await SendNotificationAsync(Actie.update, zaakResultaat, cancellationToken);

        return new CommandResult<ZaakResultaat>(zaakResultaat, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "resultaat" };
}

class UpdateZaakResultaatCommand : IRequest<CommandResult<ZaakResultaat>>
{
    public ZaakResultaat ZaakResultaat { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakUrl { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
