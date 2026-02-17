using System.Collections.Generic;
using System.Data;
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
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.BusinessRules.v1;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class CreateObjectInformatieObjectCommandHandler
    : DocumentenBaseHandler<CreateObjectInformatieObjectCommandHandler>,
        IRequestHandler<CreateObjectInformatieObjectCommand, CommandResult<ObjectInformatieObject>>
{
    private readonly DrcDbContext _context;
    private readonly IObjectInformatieObjectBusinessRuleService _objectInformatieObjectBusinessRuleService;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public CreateObjectInformatieObjectCommandHandler(
        ILogger<CreateObjectInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IObjectInformatieObjectBusinessRuleService objectInformatieObjectBusinessRuleService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _objectInformatieObjectBusinessRuleService = objectInformatieObjectBusinessRuleService;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<CommandResult<ObjectInformatieObject>> Handle(CreateObjectInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ObjectInformatieObject....");

        ValidationError error;

        var objectInformatieObject = request.ObjectInformatieObject;

        // Use ReadCommitted isolation level:
        // - FOR UPDATE provides pessimistic row-level locking (prevents concurrent modifications)
        // - xmin (configured in EnkelvoudigInformatieObject) provides optimistic concurrency detection (detects any changes since read)
        // - ReadCommitted allows better concurrency than Serializable for this use case
        // - The combination prevents both lost updates (via FOR UPDATE) and write skew (via xmin)
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var enkelvoudigInformatieObjectId = _uriService.GetId(request.InformatieObjectUrl);

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var informatieObject = await _context
            .EnkelvoudigInformatieObjecten.LockForUpdate(_context, c => c.Id, [enkelvoudigInformatieObjectId])
            .Where(rsinFilter)
            .Include(z => z.ObjectInformatieObjecten)
            .SingleOrDefaultAsync(e => e.Id == _uriService.GetId(request.InformatieObjectUrl), cancellationToken);

        if (informatieObject == null)
        {
            // The object might be locked OR not exist - check if it exists without lock
            var exists = await _context
                .EnkelvoudigInformatieObjecten.Where(rsinFilter)
                .AnyAsync(e => e.Id == enkelvoudigInformatieObjectId, cancellationToken);

            if (!exists)
            {
                // Object truly doesn't exist
                error = new ValidationError("informatieobject", ErrorCode.ObjectDoesNotExist, "Het object bestaat niet in de database.");

                return new CommandResult<ObjectInformatieObject>(null, CommandStatus.ValidationError, error);
            }

            // Object exists but is locked by another process
            error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Conflict,
                $"Het object {enkelvoudigInformatieObjectId} is vergrendeld door een andere bewerking."
            );

            return new CommandResult<ObjectInformatieObject>(null, CommandStatus.Conflict, error);
        }

        var errors = new List<ValidationError>();

        await _objectInformatieObjectBusinessRuleService.ValidateAsync(
            objectInformatieObject,
            request.InformatieObjectUrl,
            _applicationConfiguration.IgnoreZaakAndBesluitValidation,
            errors,
            cancellationToken
        );

        if (errors.Count != 0)
        {
            return new CommandResult<ObjectInformatieObject>(null, CommandStatus.ValidationError, errors.ToArray());
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            objectInformatieObject.InformatieObjectId = informatieObject.Id;
            objectInformatieObject.InformatieObject = informatieObject;
            objectInformatieObject.Owner = informatieObject.Owner;

            await _context.ObjectInformatieObjecten.AddAsync(objectInformatieObject, cancellationToken); // Note: Sequential Guid for Id is generated here by the DBMS

            audittrail.SetNew<ObjectInformatieObjectResponseDto>(objectInformatieObject);

            await audittrail.CreatedAsync(objectInformatieObject.InformatieObject, objectInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            _logger.LogDebug("ObjectInformatieObject {Id} successfully created.", objectInformatieObject.Id);
        }

        return new CommandResult<ObjectInformatieObject>(objectInformatieObject, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions => new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "objectinformatieobject" };
}

class CreateObjectInformatieObjectCommand : IRequest<CommandResult<ObjectInformatieObject>>
{
    public ObjectInformatieObject ObjectInformatieObject { get; internal set; }
    public string InformatieObjectUrl { get; internal set; }
}
