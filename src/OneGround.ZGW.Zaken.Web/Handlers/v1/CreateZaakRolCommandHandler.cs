using System;
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
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Authorization;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakRolCommandHandler : ZakenBaseHandler<CreateZaakRolCommandHandler>, IRequestHandler<CreateZaakRolCommand, CommandResult<ZaakRol>>
{
    private readonly ZrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakRolCommandHandler(
        ILogger<CreateZaakRolCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        ICatalogiServiceAgent catalogiServiceAgent,
        INotificatieService notificatieService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakRol>> Handle(CreateZaakRolCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakRol....");

        var zaakrol = request.ZaakRol;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.ZaakUrl), cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakUrl} is onbekend.");
            return new CommandResult<ZaakRol>(null, CommandStatus.NotFound, error);
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
            return new CommandResult<ZaakRol>(null, CommandStatus.Forbidden);
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakRol>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        // Query RolType to validate existance and retrieve fields Omschrijving and OmschrijvingGeneriek
        var rolType = await GetRolTypeAsync(zaakrol, errors);

        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakRol>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.RolTypen.Contains(zaakrol.RolType))
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.ZaakTypeMismatch, "De referentie hoort niet bij het zaaktype van de zaak.");

            return new CommandResult<ZaakRol>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            await _context.ZaakRollen.AddAsync(zaakrol, cancellationToken);

            zaakrol.Registratiedatum = DateTime.UtcNow;
            zaakrol.Omschrijving = rolType.Omschrijving;
            zaakrol.OmschrijvingGeneriek = Enum.Parse<OmschrijvingGeneriek>(rolType.OmschrijvingGeneriek);
            zaakrol.ZaakId = zaak.Id;
            zaakrol.Zaak = zaak;
            zaakrol.Owner = zaak.Owner;
            if (zaakrol.Vestiging != null)
                zaakrol.Vestiging.Owner = zaak.Owner;
            if (zaakrol.NietNatuurlijkPersoon != null)
                zaakrol.NietNatuurlijkPersoon.Owner = zaak.Owner;
            // Note: Rest of child-zaakrollen doesn't have owner yet!

            audittrail.SetNew<ZaakRolResponseDto>(zaakrol);

            await audittrail.CreatedAsync(zaakrol.Zaak, zaakrol, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakRol {Id} successfully created.", zaakrol.Id);
        }

        await SendNotificationAsync(Actie.create, zaakrol, cancellationToken);

        return new CommandResult<ZaakRol>(zaakrol, CommandStatus.OK);
    }

    private async Task<RolTypeResponseDto> GetRolTypeAsync(ZaakRol zaakRol, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetRolTypeByUrlAsync(zaakRol.RolType);

        if (!result.Success)
        {
            errors.Add(new ValidationError("roltype", result.Error.Code, result.Error.Title));
        }

        return result.Response;
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

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "rol" };
}

class CreateZaakRolCommand : IRequest<CommandResult<ZaakRol>>
{
    public string ZaakUrl { get; internal set; }
    public ZaakRol ZaakRol { get; internal set; }
}
