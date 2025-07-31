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
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class CreateZaakContactmomentCommandHandler
    : ZakenBaseHandler<CreateZaakContactmomentCommandHandler>,
        IRequestHandler<CreateZaakContactmomentCommand, CommandResult<ZaakContactmoment>>
{
    private readonly ZrcDbContext _context;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakContactmomentCommandHandler(
        ILogger<CreateZaakContactmomentCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakContactmoment>> Handle(CreateZaakContactmomentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakContactmoment and validating....");

        var zaakContactmoment = request.ZaakContactmoment;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context.Zaken.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakContactmoment>(null, CommandStatus.NotFound, error);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakContactmoment>(null, CommandStatus.Forbidden);
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakContactmoment>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakContactmoment>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            zaakContactmoment.ZaakId = zaak.Id;
            zaakContactmoment.Zaak = zaak;
            zaakContactmoment.Owner = zaak.Owner;

            await _context.ZaakContactmomenten.AddAsync(zaakContactmoment, cancellationToken);

            audittrail.SetNew<ZaakContactmomentResponseDto>(zaakContactmoment);

            await audittrail.CreatedAsync(zaakContactmoment.Zaak, zaakContactmoment, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakContactmoment successfully created. Id={Id}", zaakContactmoment.Id);
        }

        await SendNotificationAsync(Actie.create, zaakContactmoment, cancellationToken);

        return new CommandResult<ZaakContactmoment>(zaakContactmoment, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakcontactmoment" };
}

class CreateZaakContactmomentCommand : IRequest<CommandResult<ZaakContactmoment>>
{
    public ZaakContactmoment ZaakContactmoment { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
