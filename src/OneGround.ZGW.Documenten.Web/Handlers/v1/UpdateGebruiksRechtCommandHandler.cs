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
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Notificaties;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class UpdateGebruiksRechtCommandHandler
    : DocumentenBaseHandler<UpdateGebruiksRechtCommandHandler>,
        IRequestHandler<UpdateGebruiksRechtCommand, CommandResult<GebruiksRecht>>
{
    private readonly DrcDbContext _context;
    private readonly IEntityUpdater<GebruiksRecht> _entityUpdater;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IGenericObjectMerger _entityMerger;

    public UpdateGebruiksRechtCommandHandler(
        ILogger<UpdateGebruiksRechtCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IEntityUpdater<GebruiksRecht> entityUpdater,
        INotificatieService notificatieService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        IGenericObjectMergerFactory entityMergerFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _entityUpdater = entityUpdater;
        _auditTrailFactory = auditTrailFactory;

        _entityMerger = entityMergerFactory.Create<GebruiksRechtRequestDto>();
    }

    public async Task<CommandResult<GebruiksRecht>> Handle(UpdateGebruiksRechtCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get GebruiksRecht {Id} and validating....", request.Id);

        var errors = new List<ValidationError>();

        bool isPartialUpdate = request.PartialObject != null && request.GebruiksRecht == null;

        var rsinFilter = GetRsinFilterPredicate<GebruiksRecht>(o => o.InformatieObject.Owner == _rsin);

        var existingGebruiksRecht = await _context
            .GebruiksRechten.Include(z => z.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (existingGebruiksRecht == null)
        {
            return new CommandResult<GebruiksRecht>(null, CommandStatus.NotFound);
        }

        GebruiksRecht gebruiksrecht;
        if (isPartialUpdate)
        {
            // Partial update (e.g. for PATCH endpoint) so merge the partial object provided by the client with the existing entity
            gebruiksrecht = _entityMerger.TryMergeWithPartial(request.PartialObject, existingGebruiksRecht, errors);
            if (errors.Count != 0)
            {
                return new CommandResult<GebruiksRecht>(null, CommandStatus.ValidationError, errors.ToArray());
            }
        }
        else
        {
            // Full update (e.g. for PUT endpoint) with EnkelvoudigInformatieObjectVersie provided by the client
            gebruiksrecht = request.GebruiksRecht;
        }

        if (request.InformatieObjectUrl != null && existingGebruiksRecht.InformatieObjectId != _uriService.GetId(request.InformatieObjectUrl))
        {
            var informatieObjectRsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();
            var informatieObject = await _context
                .EnkelvoudigInformatieObjecten.Include(e => e.GebruiksRechten)
                .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                .Where(informatieObjectRsinFilter)
                .SingleOrDefaultAsync(z => z.Id == _uriService.GetId(request.InformatieObjectUrl), cancellationToken);

            if (informatieObject == null)
            {
                return new CommandResult<GebruiksRecht>(
                    null,
                    CommandStatus.ValidationError,
                    new ValidationError("informatieobject", ErrorCode.Invalid, $"InformatieObject {request.InformatieObjectUrl} is onbekend.")
                );
            }

            existingGebruiksRecht.InformatieObject = informatieObject;
            existingGebruiksRecht.InformatieObjectId = informatieObject.Id;
        }

        if (!_authorizationContext.IsAuthorized(existingGebruiksRecht.InformatieObject, AuthorizationScopes.Documenten.Update))
        {
            return new CommandResult<GebruiksRecht>(null, CommandStatus.Forbidden);
        }

        _logger.LogDebug("Updating GebruiksRecht {Id}....", existingGebruiksRecht.Id);

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<GebruiksRechtResponseDto>(existingGebruiksRecht);

            _entityUpdater.Update(gebruiksrecht, existingGebruiksRecht);

            audittrail.SetNew<GebruiksRechtResponseDto>(existingGebruiksRecht);

            if (isPartialUpdate)
            {
                await audittrail.PatchedAsync(existingGebruiksRecht.InformatieObject, existingGebruiksRecht, cancellationToken);
            }
            else
            {
                await audittrail.UpdatedAsync(existingGebruiksRecht.InformatieObject, existingGebruiksRecht, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("GebruiksRecht {Id} successfully updated.", existingGebruiksRecht.Id);
        }

        await SendNotificationAsync(Actie.update, existingGebruiksRecht, cancellationToken);

        return new CommandResult<GebruiksRecht>(existingGebruiksRecht, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "gebruiksrecht" };
}

class UpdateGebruiksRechtCommand : IRequest<CommandResult<GebruiksRecht>>
{
    public Guid Id { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
    public GebruiksRecht GebruiksRecht { get; internal set; } // For PUT endpoint, contains the full update sent by the client
    public dynamic PartialObject { get; internal set; } // For PATCH endpoint, contains the partial update sent by the client
}
