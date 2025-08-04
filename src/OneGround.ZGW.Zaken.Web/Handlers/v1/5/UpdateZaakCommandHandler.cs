using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.BusinessRules;
using OneGround.ZGW.Zaken.Web.Extensions;
using OneGround.ZGW.Zaken.Web.Notificaties;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class UpdateZaakCommandHandler : ZakenBaseHandler<UpdateZaakCommandHandler>, IRequestHandler<UpdateZaakCommand, CommandResult<Zaak>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakBusinessRuleService _zaakBusinessRuleService;
    private readonly IEntityUpdater<Zaak> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public UpdateZaakCommandHandler(
        ILogger<UpdateZaakCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IZaakBusinessRuleService zaakBusinessRuleService,
        INotificatieService notificatieService,
        IEntityUpdater<Zaak> entityUpdater,
        IAuditTrailFactory auditTrailFactory,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakBusinessRuleService = zaakBusinessRuleService;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult<Zaak>> Handle(UpdateZaakCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(request.OriginalZaak, errors))
        {
            return new CommandResult<Zaak>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        if (!await _zaakBusinessRuleService.ValidateAsync(request.OriginalZaak, request.Zaak, request.HoofdzaakUrl, errors))
        {
            return new CommandResult<Zaak>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        Geometry savedZaakgeometrie = null;

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<ZaakResponseDto>(request.OriginalZaak);

            // start tracking changes
            _context.Attach(request.OriginalZaak);

            _logger.LogDebug("Updating Zaak {zaakId}....", request.OriginalZaak.Id);

            // ZRC-014 Indien een waarde ingevuld is voor laatsteBetaaldatum en de betalingsindicatie wordt gewijzigd naar "nvt", dan MOET de laatsteBetaaldatum op null gezet worden.
            if (request.Zaak.BetalingsIndicatie == BetalingsIndicatie.nvt && request.Zaak.LaatsteBetaaldatum != null)
            {
                request.OriginalZaak.LaatsteBetaaldatum = null;
            }

            if (!string.IsNullOrEmpty(request.HoofdzaakUrl))
            {
                var hoofdzaak = await _context.Zaken.SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.HoofdzaakUrl), cancellationToken);

                if (hoofdzaak.Id != request.OriginalZaak.HoofdzaakId)
                {
                    _logger.LogDebug("Updating Hoofdzaak {HoofdzaakUrl}....", request.HoofdzaakUrl);

                    request.OriginalZaak.Hoofdzaak = hoofdzaak;
                    request.OriginalZaak.HoofdzaakId = hoofdzaak.Id;
                }
            }
            else if (request.OriginalZaak.Hoofdzaak != null)
            {
                _logger.LogDebug("Clearing existing Hoofdzaak {HoofdzaakUrl} ....", request.OriginalZaak.Hoofdzaak.Url);

                request.OriginalZaak.Hoofdzaak = null;
                request.OriginalZaak.HoofdzaakId = null;
            }

            bool geoChanges = false;
            if (request.Zaak.Zaakgeometrie != null)
            {
                geoChanges =
                    request.OriginalZaak.Zaakgeometrie == null
                    || request.Zaak.Zaakgeometrie.ToString() != request.OriginalZaak.Zaakgeometrie?.ToString();
            }

            _entityUpdater.Update(request.Zaak, request.OriginalZaak, version: 1.5M);

            // Note: We always store zaakgeometrie on RDS (28992). So we convert if SRID differs
            if (geoChanges && request.SRID.Value != 28992) // If not set in RDS then we have to convert to RDS
            {
                var conversionResult = await _context.TryConvertZaakGeometrieAsync(request.Zaak.Zaakgeometrie, cancellationToken);
                if (conversionResult.geometrie == null)
                {
                    var error = new ValidationError("zaakgeometrie", ErrorCode.Invalid, conversionResult.error);

                    return new CommandResult<Zaak>(null, CommandStatus.ValidationError, [error]);
                }

                // But we want to return the one we specified so save the original one to restore later
                savedZaakgeometrie = request.Zaak.Zaakgeometrie;

                request.OriginalZaak.Zaakgeometrie = conversionResult.geometrie;
            }

            audittrail.SetNew<ZaakResponseDto>(request.OriginalZaak);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(request.OriginalZaak, request.OriginalZaak, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(request.OriginalZaak, request.OriginalZaak, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Zaak {zaakId} successfully updated.", request.OriginalZaak.Id);

        await SendNotificationAsync(Actie.update, request.OriginalZaak, cancellationToken);

        // Restore the original Zaakgeometrie
        if (savedZaakgeometrie != null)
        {
            request.OriginalZaak.Zaakgeometrie = savedZaakgeometrie;
        }

        return new CommandResult<Zaak>(request.OriginalZaak, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaak" };
}

class UpdateZaakCommand : IRequest<CommandResult<Zaak>>
{
    public Zaak Zaak { get; internal set; }
    public Zaak OriginalZaak { get; internal set; }
    public Guid Id { get; internal set; }
    public string HoofdzaakUrl { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
    public int? SRID { get; internal set; }
}
