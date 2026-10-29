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

class CreateZaakNotitieCommandHandler
    : ZakenBaseHandler<CreateZaakNotitieCommandHandler>,
        IRequestHandler<CreateZaakNotitieCommand, CommandResult<ZaakNotitie>>
{
    private readonly ZrcDbContext _context;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakNotitieCommandHandler(
        ILogger<CreateZaakNotitieCommandHandler> logger,
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

    public async Task<CommandResult<ZaakNotitie>> Handle(CreateZaakNotitieCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakNotitie and validating....");

        var zaakNotitie = request.ZaakNotitie;

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == UriHelper.GetResourceId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("gerelateerdAan", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakNotitie>(null, CommandStatus.ValidationError, error);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakNotitie>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakNotitie>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions, zaak.LegacyAuditTrail))
        {
            zaakNotitie.ZaakId = zaak.Id;
            zaakNotitie.Zaak = zaak;
            zaakNotitie.Owner = zaak.Owner;

            await _context.ZaakNotities.AddAsync(zaakNotitie, cancellationToken);

            audittrail.SetNew<ZaakNotitieResponseDto>(zaakNotitie);

            await audittrail.CreatedAsync(zaak, zaakNotitie, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakNotitie successfully created. Id={Id}", zaakNotitie.Id);
        }

        return new CommandResult<ZaakNotitie>(zaakNotitie, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaaknotitie" };
}

class CreateZaakNotitieCommand : IRequest<CommandResult<ZaakNotitie>>
{
    public ZaakNotitie ZaakNotitie { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
