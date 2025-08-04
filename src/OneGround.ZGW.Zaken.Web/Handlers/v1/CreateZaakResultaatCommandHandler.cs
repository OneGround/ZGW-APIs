using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakResultaatCommandHandler
    : ZakenBaseHandler<CreateZaakResultaatCommandHandler>,
        IRequestHandler<CreateZaakResultaatCommand, CommandResult<ZaakResultaat>>
{
    private readonly ZrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakResultaatCommandHandler(
        ILogger<CreateZaakResultaatCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        ICatalogiServiceAgent catalogiServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakResultaat>> Handle(CreateZaakResultaatCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakResultaat and validating....");

        var zaakResultaat = request.ZaakResultaat;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .Include(z => z.Resultaat)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakResultaat>(null, CommandStatus.NotFound, error);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakResultaat>(null, CommandStatus.Forbidden);
        }

        if (zaak.Einddatum.HasValue && !_authorizationContext.IsAuthorized(zaak, AuthorizationScopes.Zaken.ForcedUpdate))
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.Unique, "Het resultaattype mag niet gewijzigd worden");
            errors.Add(error);
        }

        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakResultaat>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.ResultaatTypen.Contains(zaakResultaat.ResultaatType))
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.ZaakTypeMismatch, "De referentie hoort niet bij het zaaktype van de zaak.");

            return new CommandResult<ZaakResultaat>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            await _context.ZaakResultaten.AddAsync(zaakResultaat, cancellationToken);

            zaakResultaat.ZaakId = zaak.Id;
            zaakResultaat.Zaak = zaak;
            zaakResultaat.Owner = zaak.Owner;

            zaak.Resultaat = zaakResultaat;

            audittrail.SetNew<ZaakResultaatResponseDto>(zaak.Resultaat);

            await audittrail.CreatedAsync(zaakResultaat.Zaak, zaakResultaat, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("ZaakResultaat {Id} successfully created.", zaakResultaat.Id);

        await SendNotificationAsync(Actie.create, zaakResultaat, cancellationToken);

        return new CommandResult<ZaakResultaat>(zaakResultaat, CommandStatus.OK);
    }

    private async Task<ZaakTypeResponseDto> GetZaakTypeAsync(string zaakTypeUrl, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetZaakTypeByUrlAsync(zaakTypeUrl);

        if (!result.Success)
        {
            errors.Add(new ValidationError("zaaktype", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "resultaat" };
}

class CreateZaakResultaatCommand : IRequest<CommandResult<ZaakResultaat>>
{
    public ZaakResultaat ZaakResultaat { get; internal set; }
    public string ZaakUrl { get; internal set; }
}
