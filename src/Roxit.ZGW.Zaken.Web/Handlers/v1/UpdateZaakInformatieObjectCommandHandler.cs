using System;
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
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.Contracts.v1.Responses;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.BusinessRules;
using Roxit.ZGW.Zaken.Web.Notificaties;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class UpdateZaakInformatieObjectCommandHandler
    : ZakenBaseHandler<UpdateZaakInformatieObjectCommandHandler>,
        IRequestHandler<UpdateZaakInformatieObjectCommand, CommandResult<ZaakInformatieObject>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakInformatieObjectBusinessRuleService _zaakInformatieObjectBusinessRuleService;
    private readonly IEntityUpdater<ZaakInformatieObject> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IClosedZaakModificationBusinessRule _closedZaakModificationBusinessRule;

    public UpdateZaakInformatieObjectCommandHandler(
        ILogger<UpdateZaakInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IZaakInformatieObjectBusinessRuleService zaakInformatieObjectBusinessRuleService,
        INotificatieService notificatieService,
        IEntityUpdater<ZaakInformatieObject> entityUpdater,
        IAuditTrailFactory auditTrailFactory,
        IClosedZaakModificationBusinessRule closedZaakModificationBusinessRule,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService)
    {
        _context = context;
        _zaakInformatieObjectBusinessRuleService = zaakInformatieObjectBusinessRuleService;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _closedZaakModificationBusinessRule = closedZaakModificationBusinessRule;
    }

    public async Task<CommandResult<ZaakInformatieObject>> Handle(UpdateZaakInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakInformatieObject {Id} and validating....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakInformatieObject>();

        var zaakInformatieObject = await _context
            .ZaakInformatieObjecten.Where(rsinFilter)
            .Include(z => z.Zaak)
            .ThenInclude(z => z.Kenmerken)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakInformatieObject == null)
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.NotFound);
        }

        var errors = new List<ValidationError>();

        if (!_closedZaakModificationBusinessRule.ValidateClosedZaakModificationRule(zaakInformatieObject.Zaak, errors))
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.Forbidden, errors.ToArray());
        }

        if (
            !await _zaakInformatieObjectBusinessRuleService.ValidateAsync(zaakInformatieObject, request.ZaakInformatieObject, request.ZaakUrl, errors)
        )
        {
            return new CommandResult<ZaakInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Updating ZaakResultaat {Id}....", zaakInformatieObject.Id);

            audittrail.SetOld<ZaakInformatieObjectResponseDto>(zaakInformatieObject);

            _logger.LogDebug("Updating ZaakInformatieObject {Id}....", zaakInformatieObject.Id);

            _entityUpdater.Update(request.ZaakInformatieObject, zaakInformatieObject);

            audittrail.SetNew<ZaakInformatieObjectResponseDto>(zaakInformatieObject);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(zaakInformatieObject.Zaak, zaakInformatieObject, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(zaakInformatieObject.Zaak, zaakInformatieObject, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("ZaakInformatieObject {Id} successfully updated.", zaakInformatieObject.Id);
        }

        await SendNotificationAsync(Actie.update, zaakInformatieObject, cancellationToken);

        return new CommandResult<ZaakInformatieObject>(zaakInformatieObject, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.ZRC, Resource = "zaakinformatieobject" };
}

class UpdateZaakInformatieObjectCommand : IRequest<CommandResult<ZaakInformatieObject>>
{
    public ZaakInformatieObject ZaakInformatieObject { get; internal set; }
    public Guid Id { get; internal set; }
    public string ZaakUrl { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
