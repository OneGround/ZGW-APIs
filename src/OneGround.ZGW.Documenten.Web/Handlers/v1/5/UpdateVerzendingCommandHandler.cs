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
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;
        _verzendingBusinessRuleService = verzendingBusinessRuleService;
    }

    public async Task<CommandResult<Verzending>> Handle(UpdateVerzendingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Verzending {requestId} and validating....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Verzending>(o => o.InformatieObject.Owner == _rsin);

        var informatieObjectRsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();
        var informatieObject = await _context
            .EnkelvoudigInformatieObjecten.Include(e => e.Verzendingen)
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

        var verzending = await _context
            .Verzendingen.Include(z => z.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (verzending == null)
        {
            return new CommandResult<Verzending>(null, CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(verzending.InformatieObject, AuthorizationScopes.Documenten.Update))
        {
            return new CommandResult<Verzending>(null, CommandStatus.Forbidden);
        }

        var errors = new List<ValidationError>();

        var informatieObjectUrl = _uriService.GetUri(verzending.InformatieObject);

        if (request.InformatieObjectUrl != informatieObjectUrl)
        {
            verzending.InformatieObject = informatieObject;
            verzending.InformatieObjectId = informatieObject.Id;
        }

        _verzendingBusinessRuleService.Validate(informatieObject, request.Verzending, request.Id, errors);

        if (errors.Count != 0)
        {
            return new CommandResult<Verzending>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        _logger.LogDebug("Updating Verzending {verzendingId}....", verzending.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<VerzendingResponseDto>(verzending);

            _entityUpdater.Update(request.Verzending, verzending);

            audittrail.SetNew<VerzendingResponseDto>(verzending);

            if (request.IsPartialUpdate)
            {
                await audittrail.PatchedAsync(verzending.InformatieObject, verzending, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(verzending.InformatieObject, verzending, cancellationToken);
            }

            // TODO: DELETE!!!!!!!!!!!!!
            await Task.Delay(15000);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Verzending {verzendingId} successfully updated.", verzending.Id);
        }

        await SendNotificationAsync(Actie.update, verzending, cancellationToken);

        return new CommandResult<Verzending>(verzending, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "Verzending" };
}

class UpdateVerzendingCommand : IRequest<CommandResult<Verzending>>
{
    public Verzending Verzending { get; internal set; }
    public Guid Id { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
    public bool IsPartialUpdate { get; internal set; }
}
