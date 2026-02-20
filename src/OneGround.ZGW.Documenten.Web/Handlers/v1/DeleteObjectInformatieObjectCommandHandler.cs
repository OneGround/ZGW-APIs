using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Constants;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class DeleteObjectInformatieObjectCommandHandler
    : DocumentenBaseHandler<DeleteObjectInformatieObjectCommandHandler>,
        IRequestHandler<DeleteObjectInformatieObjectCommand, CommandResult>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public DeleteObjectInformatieObjectCommandHandler(
        ILogger<DeleteObjectInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult> Handle(DeleteObjectInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ObjectInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ObjectInformatieObject>(o => o.InformatieObject.Owner == _rsin);

        var objectInformatieObject = await _context
            .ObjectInformatieObjecten.Where(rsinFilter)
            .Include(o => o.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (objectInformatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        var informatieObject = objectInformatieObject.InformatieObject;
        if (informatieObject == null)
        {
            return new CommandResult(CommandStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(objectInformatieObject.InformatieObject))
        {
            return new CommandResult(CommandStatus.Forbidden);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            _logger.LogDebug("Deleting ObjectInformatieObject {Id}....", objectInformatieObject.Id);

            audittrail.SetOld<ObjectInformatieObjectResponseDto>(objectInformatieObject);

            await _context.ObjectInformatieObjecten.Where(x => x.Id == objectInformatieObject.Id).ExecuteDeleteAsync(cancellationToken);

            await audittrail.DestroyedAsync(informatieObject, objectInformatieObject, cancellationToken);

            _logger.LogDebug("ObjectInformatieObject {Id} successfully deleted.", objectInformatieObject.Id);
        }

        await transaction.CommitAsync(cancellationToken);

        return new CommandResult(CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new() { Bron = ServiceRoleName.DRC, Resource = "objectinformatieobject" };
}

class DeleteObjectInformatieObjectCommand : IRequest<CommandResult>
{
    public Guid Id { get; set; }
}
