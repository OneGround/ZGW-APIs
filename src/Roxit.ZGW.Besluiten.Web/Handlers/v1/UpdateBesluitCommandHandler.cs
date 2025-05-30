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
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.Authorization;
using Roxit.ZGW.Besluiten.Web.BusinessRules;
using Roxit.ZGW.Besluiten.Web.Notificaties;
using Roxit.ZGW.Common.Constants;
using Roxit.ZGW.Common.Contracts.v1;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.AuditTrail;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Besluiten.Web.Handlers.v1;

class UpdateBesluitCommandHandler : BesluitenBaseHandler<UpdateBesluitCommandHandler>, IRequestHandler<UpdateBesluitCommand, CommandResult<Besluit>>
{
    private readonly BrcDbContext _context;
    private readonly IBesluitBusinessRuleService _besluitBusinessRuleService;
    private readonly IEntityUpdater<Besluit> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public UpdateBesluitCommandHandler(
        ILogger<UpdateBesluitCommandHandler> logger,
        IConfiguration configuration,
        BrcDbContext context,
        IEntityUriService uriService,
        IBesluitBusinessRuleService besluitBusinessRuleService,
        INotificatieService notificatieService,
        IEntityUpdater<Besluit> entityUpdater,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
        _besluitBusinessRuleService = besluitBusinessRuleService;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<Besluit>> Handle(UpdateBesluitCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Besluit {Id} and validating....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context.Besluiten.Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluit == null)
        {
            return new CommandResult<Besluit>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(besluit))
        {
            return new CommandResult<Besluit>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        if (!await _besluitBusinessRuleService.ValidateAsync(besluit, request.Besluit, _applicationConfiguration.IgnoreZaakValidation, errors))
        {
            return new CommandResult<Besluit>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<BesluitResponseDto>(besluit);

            _logger.LogDebug("Updating Besluit {Id}....", request.Id);

            _entityUpdater.Update(request.Besluit, besluit);

            audittrail.SetNew<BesluitResponseDto>(besluit);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(besluit, besluit, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(besluit, besluit, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogDebug("Besluit {Id} successfully updated.", request.Id);

        await SendNotificationAsync(Actie.update, besluit, cancellationToken);

        return new CommandResult<Besluit>(besluit, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.BRC, Resource = "besluit" };
}

class UpdateBesluitCommand : IRequest<CommandResult<Besluit>>
{
    public Besluit Besluit { get; internal set; }
    public Guid Id { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
