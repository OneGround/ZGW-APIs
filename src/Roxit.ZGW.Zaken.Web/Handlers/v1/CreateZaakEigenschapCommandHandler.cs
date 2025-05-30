using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.ServiceAgent.v1;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;
using Roxit.ZGW.Zaken.Web.Validators.v1;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class CreateZaakEigenschapCommandHandler
    : ZakenBaseHandler<CreateZaakEigenschapCommandHandler>,
        IRequestHandler<CreateZaakEigenschapCommand, CommandResult<ZaakEigenschap>>
{
    private readonly ZrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateZaakEigenschapCommandHandler(
        ILogger<CreateZaakEigenschapCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        ICatalogiServiceAgent catalogiServiceAgent,
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

    public async Task<CommandResult<ZaakEigenschap>> Handle(CreateZaakEigenschapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ZaakEigenschap and validating....");

        if (request.ZaakEigenschap.ZaakId != request.ZaakId)
        {
            return new CommandResult<ZaakEigenschap>(
                null,
                CommandStatus.ValidationError,
                new ValidationError(
                    "zaak",
                    ErrorCode.Invalid,
                    "Zaak-resource in Zaakeigenschap request wijst niet naar de zaak waaraan de eigenschap toegevoegd gaat worden."
                )
            );
        }

        var zaakEigenschap = request.ZaakEigenschap;

        var errors = new List<ValidationError>();

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.Where(rsinFilter)
            .Include(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.ZaakId, cancellationToken);

        if (zaak == null)
        {
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakId} is onbekend.");
            errors.Add(error);
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (zaak.Id != request.ZaakId)
        {
            // TODO: find out if this should be validated
            var error = new ValidationError("zaak", ErrorCode.Invalid, $"Zaak {request.ZaakId} is onbekend.");
            errors.Add(error);
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
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.Forbidden);
        }

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaak, errors))
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        var eigenschap = await GetEigenschapAsync(zaakEigenschap, errors);
        var zaakType = await GetZaakTypeAsync(zaak.Zaaktype, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        if (!zaakType.Eigenschappen.Contains(eigenschap.Url))
        {
            var error = new ValidationError("nonFieldErrors", ErrorCode.ZaakTypeMismatch, "De referentie hoort niet bij het zaaktype van de zaak.");

            return new CommandResult<ZaakEigenschap>(null, CommandStatus.ValidationError, error);
        }

        if (!ZaakEigenschapValidator.Validate(request.ZaakEigenschap, eigenschap.Specificatie, out var error2))
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.ValidationError, error2);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            await _context.ZaakEigenschappen.AddAsync(zaakEigenschap, cancellationToken); // Note: Sequential Guid for Id is generated here by EF

            zaakEigenschap.Naam = eigenschap.Naam;
            zaakEigenschap.Zaak = zaak;
            zaakEigenschap.ZaakId = request.ZaakId;
            zaakEigenschap.Owner = zaak.Owner;

            audittrail.SetNew<Zaken.Contracts.v1.Responses.ZaakEigenschapResponseDto>(zaakEigenschap);

            await audittrail.CreatedAsync(zaakEigenschap.Zaak, zaakEigenschap, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakEigenschap {Id} successfully created.", zaakEigenschap.Id);
        }

        await SendNotificationAsync(Actie.create, zaakEigenschap, cancellationToken);

        return new CommandResult<ZaakEigenschap>(zaakEigenschap, CommandStatus.OK);
    }

    private async Task<EigenschapResponseDto> GetEigenschapAsync(ZaakEigenschap eigenschap, List<ValidationError> errors)
    {
        var result = await _catalogiServiceAgent.GetEigenschapByUrlAsync(eigenschap.Eigenschap);

        if (!result.Success)
        {
            errors.Add(new ValidationError("eigenschap", result.Error.Code, result.Error.Title));
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

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakeigenschap" };
}

class CreateZaakEigenschapCommand : IRequest<CommandResult<ZaakEigenschap>>
{
    public Guid ZaakId { get; internal set; }
    public ZaakEigenschap ZaakEigenschap { get; internal set; }
}
