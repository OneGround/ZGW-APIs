using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Besluiten.Contracts.v1.Responses;
using Roxit.ZGW.Besluiten.ServiceAgent.v1;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakBesluitCommandHandler
    : ZakenBaseHandler<CreateZaakBesluitCommandHandler>,
        IRequestHandler<CreateZaakBesluitCommand, CommandResult<ZaakBesluit>>
{
    private readonly ZrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IBesluitenServiceAgent _besluitenServiceAgent;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakBesluitCommandHandler(
        ILogger<CreateZaakBesluitCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        ICatalogiServiceAgent catalogiServiceAgent,
        IBesluitenServiceAgent besluitenServiceAgent,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _besluitenServiceAgent = besluitenServiceAgent;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ZaakBesluit>> Handle(CreateZaakBesluitCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakBesluit and validating....");

        var zaakBesluit = request.Besluit;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.ZaakId, cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakId} is onbekend.");
            return new CommandResult<ZaakBesluit>(null, CommandStatus.ValidationError, error);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new CommandResult<ZaakBesluit>(null, CommandStatus.Forbidden);
        }

        var besluit = await GetBesluitAsync(zaakBesluit, errors);
        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakBesluit>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.BesluitTypen.Contains(besluit.BesluitType))
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.ZaakTypeMismatch, "De referentie hoort niet bij het zaaktype van de zaak.");

            return new CommandResult<ZaakBesluit>(null, CommandStatus.ValidationError, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            await _context.ZaakBesluiten.AddAsync(zaakBesluit, cancellationToken); // Note: Sequential Guid for Id is generated here by EF

            zaakBesluit.Besluit = request.Besluit.Besluit;
            zaakBesluit.Zaak = zaak;
            zaakBesluit.ZaakId = request.ZaakId;

            audittrail.SetNew<ZaakBesluitResponseDto>(zaakBesluit);

            await audittrail.CreatedAsync(zaakBesluit.Zaak, zaakBesluit, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakBesluit {Id} successfully created.", zaakBesluit.Id);
        }

        await SendNotificationAsync(Actie.create, zaakBesluit, cancellationToken);

        return new CommandResult<ZaakBesluit>(zaakBesluit, CommandStatus.OK);
    }

    private async Task<BesluitResponseDto> GetBesluitAsync(ZaakBesluit zaakBesluit, List<ValidationError> errors)
    {
        var result = await _besluitenServiceAgent.GetBesluitByUrlAsync(zaakBesluit.Besluit);
        if (!result.Success)
        {
            errors.Add(new ValidationError("besluit", result.Error.Code, result.Error.Title));
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

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakbesluit" };
}

class CreateZaakBesluitCommand : IRequest<CommandResult<ZaakBesluit>>
{
    public Guid ZaakId { get; internal set; }
    public ZaakBesluit Besluit { get; internal set; }
}
