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

class CreateZaakVerzoekCommandHandler
    : ZakenBaseHandler<CreateZaakVerzoekCommandHandler>,
        IRequestHandler<CreateZaakVerzoekCommand, CommandResult<ZaakVerzoek>>
{
    private readonly ZrcDbContext _context;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakVerzoekCommandHandler(
        ILogger<CreateZaakVerzoekCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IEntityUriService uriService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakVerzoek>> Handle(CreateZaakVerzoekCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakVerzoek and validating....");

        var zaakVerzoek = request.ZaakVerzoek;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context.Zaken.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakVerzoek>(null, CommandStatus.NotFound, error);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakVerzoek>(null, CommandStatus.Forbidden);
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakVerzoek>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakVerzoek>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            zaakVerzoek.ZaakId = zaak.Id;
            zaakVerzoek.Zaak = zaak;
            zaakVerzoek.Owner = zaak.Owner;

            await _context.ZaakVerzoeken.AddAsync(zaakVerzoek, cancellationToken);

            audittrail.SetNew<ZaakVerzoekResponseDto>(zaakVerzoek);

            await audittrail.CreatedAsync(zaakVerzoek.Zaak, zaakVerzoek, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakVerzoek successfully created. Id={Id}", zaakVerzoek.Id);
        }

        await SendNotificationAsync(Actie.create, zaakVerzoek, cancellationToken);

        return new CommandResult<ZaakVerzoek>(zaakVerzoek, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakverzoek" };
}

class CreateZaakVerzoekCommand : IRequest<CommandResult<ZaakVerzoek>>
{
    public ZaakVerzoek ZaakVerzoek { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
