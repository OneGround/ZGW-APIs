using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1._5;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

class UpdateVerzendingCommandHandler
    : DocumentenBaseHandler<UpdateVerzendingCommandHandler>,
        IRequestHandler<UpdateVerzendingCommand, CommandResult<Verzending>>
{
    private readonly DrcDbContext _context;
    private readonly IEntityUpdater<Verzending> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IVerzendingBusinessRuleService _verzendingBusinessRuleService;
    private readonly IGenericObjectMerger _entityMerger;

    public UpdateVerzendingCommandHandler(
        ILogger<UpdateVerzendingCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IEntityUpdater<Verzending> entityUpdater,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IVerzendingBusinessRuleService verzendingBusinessRuleService,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        IGenericObjectMergerFactory entityMergerFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _verzendingBusinessRuleService = verzendingBusinessRuleService;

        _entityMerger = entityMergerFactory.Create<VerzendingRequestDto>();
    }

    public async Task<CommandResult<Verzending>> Handle(UpdateVerzendingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Verzending {requestId} and validating....", request.Id);

        var errors = new List<ValidationError>();

        bool isPartialUpdate = request.PartialObject != null && request.Verzending == null;

        var rsinFilter = GetRsinFilterPredicate<Verzending>(o => o.InformatieObject.Owner == _rsin);

        var existingVerzending = await _context
            .Verzendingen.Include(z => z.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (existingVerzending == null)
        {
            return new CommandResult<Verzending>(null, CommandStatus.NotFound);
        }

        Verzending verzending;
        if (isPartialUpdate)
        {
            // Partial update (e.g. for PATCH endpoint) so merge the partial object provided by the client with the existing entity
            verzending = _entityMerger.TryMergeWithPartial(request.PartialObject, existingVerzending, errors);
            if (errors.Count != 0)
            {
                return new CommandResult<Verzending>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }
        else
        {
            // Full update (e.g. for PUT endpoint) with EnkelvoudigInformatieObjectVersie provided by the client
            verzending = request.Verzending;
        }

        if (request.InformatieObjectUrl != null && existingVerzending.InformatieObjectId != _uriService.GetId(request.InformatieObjectUrl))
        {
            var informatieObjectRsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();
            var informatieObject = await _context
                .EnkelvoudigInformatieObjecten.Include(e => e.Verzendingen)
                .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                .Where(informatieObjectRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.InformatieObjectUrl), cancellationToken);

            if (informatieObject == null)
            {
                return new CommandResult<Verzending>(
                    null,
                    CommandStatus.ValidationError,
                    new ValidationError("informatieobject", ErrorCode.Invalid, $"InformatieObject {request.InformatieObjectUrl} is onbekend.")
                );
            }

            existingVerzending.InformatieObject = informatieObject;
            existingVerzending.InformatieObjectId = informatieObject.Id;
        }

        if (!_authorizationContext.IsAuthorized(existingVerzending.InformatieObject, AuthorizationScopes.Documenten.Update))
        {
            return new CommandResult<Verzending>(null, CommandStatus.Forbidden);
        }

        _verzendingBusinessRuleService.Validate(existingVerzending.InformatieObject, verzending, request.Id, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<Verzending>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating Verzending {verzendingId}....", existingVerzending.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<VerzendingResponseDto>(existingVerzending);

            _entityUpdater.Update(verzending, existingVerzending);

            audittrail.SetNew<VerzendingResponseDto>(existingVerzending);

            if (isPartialUpdate)
            {
                await audittrail.PatchedAsync(existingVerzending.InformatieObject, existingVerzending, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(existingVerzending.InformatieObject, existingVerzending, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Verzending {verzendingId} successfully updated.", existingVerzending.Id);
        }

        await SendNotificationAsync(Actie.update, existingVerzending, cancellationToken);

        return new CommandResult<Verzending>(existingVerzending, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "Verzending" };
}

class UpdateVerzendingCommand : IRequest<CommandResult<Verzending>>
{
    public Guid Id { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
    public Verzending Verzending { get; internal set; } // For PUT endpoint, contains the full update sent by the client
    public dynamic PartialObject { get; internal set; } // For PATCH endpoint, contains the partial update sent by the client
}
