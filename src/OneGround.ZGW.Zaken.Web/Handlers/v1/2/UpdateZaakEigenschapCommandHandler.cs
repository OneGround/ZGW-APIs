using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.ServiceAgent.v1;
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
using OneGround.ZGW.Zaken.Web.Validators.v1;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._2;

class UpdateZaakEigenschapCommandHandler
    : ZakenBaseHandler<UpdateZaakEigenschapCommandHandler>,
        IRequestHandler<UpdateZaakEigenschapCommand, CommandResult<ZaakEigenschap>>
{
    private readonly ZrcDbContext _context;
    private readonly ICatalogiServiceAgent _catalogiServiceAgent;
    private readonly IEntityUpdater<ZaakEigenschap> _entityUpdater;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateZaakEigenschapCommandHandler(
        ILogger<UpdateZaakEigenschapCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        ICatalogiServiceAgent catalogiServiceAgent,
        IEntityUriService uriService,
        INotificatieService notificatieService,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IEntityUpdater<ZaakEigenschap> entityUpdater,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _catalogiServiceAgent = catalogiServiceAgent;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult<ZaakEigenschap>> Handle(UpdateZaakEigenschapCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakEigenschap {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakEigenschap>();

        var zaakEigenschap = await _context
            .ZaakEigenschappen.Where(rsinFilter)
            .Include(z => z.Zaak)
            .ThenInclude(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakEigenschap == null)
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.NotFound);
        }

        if (zaakEigenschap.ZaakId != request.ZaakEigenschap.ZaakId)
        {
            return new CommandResult<ZaakEigenschap>(
                null,
                CommandStatus.ValidationError,
                new ValidationError(
                    "zaak",
                    ErrorCode.Invalid,
                    "Zaak-resource in Zaakeigenschap request wijst niet naar de zaak waarin de eigenschap bewerkt wordt."
                )
            );
        }

        if (!_authorizationContext.IsAuthorized(zaakEigenschap.Zaak))
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakEigenschap.Zaak, errors))
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        var eigenschap = await GetEigenschapAsync(zaakEigenschap, errors);

        if (!ZaakEigenschapValidator.Validate(request.ZaakEigenschap, eigenschap.Specificatie, out var error2))
        {
            return new CommandResult<ZaakEigenschap>(null, CommandStatus.ValidationError, error2);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Updating ZaakEigenschap {Id}....", zaakEigenschap.Id);

            audittrail.SetOld<ZaakEigenschapResponseDto>(zaakEigenschap);

            _entityUpdater.Update(request.ZaakEigenschap, zaakEigenschap);

            audittrail.SetNew<ZaakEigenschapResponseDto>(zaakEigenschap);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakEigenschap.Zaak, zaakEigenschap, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakEigenschap.Zaak, zaakEigenschap, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakEigenschap {Id} successfully updated.", zaakEigenschap.Id);
        }

        await SendNotificationAsync(Actie.update, zaakEigenschap, cancellationToken);

        return new CommandResult<ZaakEigenschap>(zaakEigenschap, CommandStatus.OK);
    }

    private async Task<Catalogi.Contracts.v1.Responses.EigenschapResponseDto> GetEigenschapAsync(
        ZaakEigenschap eigenschap,
        List<ValidationError> errors
    )
    {
        var result = await _catalogiServiceAgent.GetEigenschapByUrlAsync(eigenschap.Eigenschap);

        if (!result.Success)
        {
            errors.Add(new ValidationError("eigenschap", result.Error.Code, result.Error.Title));
        }

        return result.Response;
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakeigenschap" };
}

class UpdateZaakEigenschapCommand : IRequest<CommandResult<ZaakEigenschap>>
{
    public ZaakEigenschap ZaakEigenschap { get; internal set; }
    public Guid ZaakId { get; internal set; }
    public Guid Id { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
