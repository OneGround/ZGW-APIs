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
using Roxit.ZGW.Zaken.Contracts.v1.Responses.ZaakObject;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakObjectCommandHandler
    : ZakenBaseHandler<CreateZaakObjectCommandHandler>,
        IRequestHandler<CreateZaakObjectCommand, CommandResult<ZaakObject>>
{
    private readonly ZrcDbContext _context;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakObjectCommandHandler(
        ILogger<CreateZaakObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakObject>> Handle(CreateZaakObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakObject....");

        var zaakObject = request.ZaakObject;

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakObject>(null, CommandStatus.ValidationError, error);
        }

        if (
            !_authorizationContext.IsAuthorized(
                zaak.Zaaktype,
                zaak.VertrouwelijkheidAanduiding,
                AuthorizationScopes.Zaken.Update,
                AuthorizationScopes.Zaken.ForcedUpdate
            )
        )
        {
            return new CommandResult<ZaakObject>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakObject>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            await _context.ZaakObjecten.AddAsync(zaakObject, cancellationToken);

            zaakObject.ZaakId = zaak.Id;
            zaakObject.Zaak = zaak;
            zaakObject.Owner = zaak.Owner;

            if (zaakObject.Adres != null)
                zaakObject.Adres.Owner = zaak.Owner;
            if (zaakObject.Buurt != null)
                zaakObject.Buurt.Owner = zaak.Owner;
            if (zaakObject.Gemeente != null)
                zaakObject.Gemeente.Owner = zaak.Owner;
            if (zaakObject.KadastraleOnroerendeZaak != null)
                zaakObject.KadastraleOnroerendeZaak.Owner = zaak.Owner;
            if (zaakObject.Overige != null)
                zaakObject.Overige.Owner = zaak.Owner;
            if (zaakObject.Pand != null)
                zaakObject.Pand.Owner = zaak.Owner;
            if (zaakObject.TerreinGebouwdObject != null)
                zaakObject.TerreinGebouwdObject.Owner = zaak.Owner;
            if (zaakObject.WozWaardeObject != null)
                zaakObject.WozWaardeObject.Owner = zaak.Owner;

            audittrail.SetNew<ZaakObjectResponseDto>(zaakObject);

            await audittrail.CreatedAsync(zaakObject.Zaak, zaakObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakObject {Id} successfully created.", zaakObject.Id);
        }

        await SendNotificationAsync(Actie.create, zaakObject, cancellationToken);

        return new CommandResult<ZaakObject>(zaakObject, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakobject" };
}

class CreateZaakObjectCommand : IRequest<CommandResult<ZaakObject>>
{
    public string ZaakUrl { get; internal set; }
    public ZaakObject ZaakObject { get; internal set; }
}
