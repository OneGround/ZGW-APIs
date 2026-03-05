using System;
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
using OneGround.ZGW.Documenten.Web.Authorization;
using OneGround.ZGW.Documenten.Web.Concurrency;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class LockEnkelvoudigInformatieObjectCommandHandler
    : DocumentenBaseHandler<LockEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<LockEnkelvoudigInformatieObjectCommand, CommandResult<string>>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly ResilienceConcurrencyRetryPipeline<EnkelvoudigInformatieObject> _concurrencyRetryPipeline;

    public LockEnkelvoudigInformatieObjectCommandHandler(
        ILogger<LockEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        ResilienceConcurrencyRetryPipeline<EnkelvoudigInformatieObject> concurrencyRetryPipeline
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
        _concurrencyRetryPipeline = concurrencyRetryPipeline;
    }

    public async Task<CommandResult<string>> Handle(LockEnkelvoudigInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        if (request.Set)
            _logger.LogDebug("Locking EnkelvoudigInformatieObject....");
        else
            _logger.LogDebug("Unlocking EnkelvoudigInformatieObject....");

        // Use ReadCommitted isolation level:
        // - FOR UPDATE provides pessimistic row-level locking (prevents concurrent modifications)
        // - xmin (configured in EnkelvoudigInformatieObject) provides optimistic concurrency detection (detects any changes since read)
        // - ReadCommitted allows better concurrency than Serializable for this use case
        // - The combination prevents both lost updates (via FOR UPDATE) and write skew (via xmin)
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var (enkelvoudigInformatieObject, status) = await _concurrencyRetryPipeline.ExecuteWithResultAsync(
            async (token) =>
            {
                // First, try to acquire lock on the EnkelvoudigInformatieObject
                var _enkelvoudigInformatieObject = await _context
                    .EnkelvoudigInformatieObjecten.LockForUpdate(_context, c => c.Id, [request.Id])
                    .Where(rsinFilter)
                    .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                    .SingleOrDefaultAsync(e => e.Id == request.Id, token);

                // The object might be locked OR not exist - check if it exists without lock
                if (_enkelvoudigInformatieObject == null)
                {
                    // The object might be locked OR not exist - check if it exists without lock
                    var exists = await _context.EnkelvoudigInformatieObjecten.Where(rsinFilter).AnyAsync(e => e.Id == request.Id, token);

                    if (!exists)
                    {
                        // Object truly doesn't exist
                        return (enkelvoudiginformatieobject: null, status: CommandStatus.NotFound);
                    }

                    // Throw the exception again so Polly knows a retry is needed (giving up after maximum reached retries)
                    throw new ConcurrencyConflictException("Concurrency conflict detected.", request.Id);
                }
                else
                {
                    return (enkelvoudiginformatieobject: _enkelvoudigInformatieObject, status: CommandStatus.OK);
                }
            },
            cancellationToken
        );

        if (status == CommandStatus.NotFound)
        {
            return new CommandResult<string>(null, CommandStatus.NotFound);
        }

        if (status == CommandStatus.Conflict)
        {
            // Object exists but is locked by another process
            var error = new ValidationError(
                "nonFieldErrors",
                ErrorCode.Conflict,
                $"Het enkelvoudiginformatieobject {request.Id} is vergrendeld door een andere bewerking."
            );
            return new CommandResult<string>(null, CommandStatus.Conflict, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(enkelvoudigInformatieObject);

            if (request.Set)
            {
                if (enkelvoudigInformatieObject.Locked)
                {
                    var error = new ValidationError("nonFieldErrors", ErrorCode.ExistingLock, "Het document is al gelockt.");
                    return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                }

                enkelvoudigInformatieObject.Locked = true;
                enkelvoudigInformatieObject.Lock = Guid.NewGuid().ToString().Replace("-", "");
            }
            else
            {
                if (request.Lock != null && enkelvoudigInformatieObject.Lock != request.Lock)
                {
                    var error = new ValidationError("nonFieldErrors", ErrorCode.IncorrectLockId, "Incorrect lock ID.");
                    return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                }

                if (request.Lock == null)
                {
                    if (!AuthorizationContextAccessor.AuthorizationContext.IsForcedUnlockAuthorized())
                    {
                        var error = new ValidationError("nonFieldErrors", ErrorCode.MissingLockId, "Dit is een verplicht veld.");
                        return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                    }
                }
                enkelvoudigInformatieObject.Locked = false;
                enkelvoudigInformatieObject.Lock = null;
            }

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(enkelvoudigInformatieObject);

            await audittrail.PatchedAsync(enkelvoudigInformatieObject, enkelvoudigInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            if (request.Set)
                _logger.LogDebug("EnkelvoudigInformatieObject successfully locked. Lock={Lock}", enkelvoudigInformatieObject.Lock);
            else
                _logger.LogDebug("EnkelvoudigInformatieObject successfully unlocked.");
        }
        return new CommandResult<string>(enkelvoudigInformatieObject.Lock, CommandStatus.OK);
    }

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };
}

class LockEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<string>>
{
    public bool Set { get; internal set; }
    public string Lock { get; internal set; }
    public Guid Id { get; internal set; }
}
