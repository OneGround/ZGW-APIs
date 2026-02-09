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
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.Contracts.v1._1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._1;

public class LockEnkelvoudigInformatieObjectCommandHandler
    : DocumentenBaseHandler<LockEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<LockEnkelvoudigInformatieObjectCommand, CommandResult<string>>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IDocumentService _documentService;

    public LockEnkelvoudigInformatieObjectCommandHandler(
        ILogger<LockEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        IEntityUriService uriService,
        IAuditTrailFactory auditTrailFactory,
        IAuthorizationContextAccessor authorizationContextAccessor,
        INotificatieService notificatieService,
        IDocumentServicesResolver documentServicesResolver,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService, documentKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;

        _documentService = documentServicesResolver.GetDefault();
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

        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.LockForUpdate(_context, c => c.Id, [request.Id])
            .Where(rsinFilter)
            .Include(e => e.EnkelvoudigInformatieObjectVersies)
            .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        ValidationError error;

        // The object might be locked OR not exist - check if it exists without lock
        if (enkelvoudigInformatieObject == null)
        {
            // The object might be locked OR not exist - check if it exists without lock
            var exists = await _context.EnkelvoudigInformatieObjecten.AnyAsync(e => e.Id == request.Id, cancellationToken);

            if (!exists)
            {
                // Object truly doesn't exist
                error = new ValidationError("id", ErrorCode.NotFound, $"EnkelvoudigInformatieObject {request.Id} is onbekend.");

                return new CommandResult<string>(null, CommandStatus.NotFound, error);
            }

            // Object exists but is locked by another process
            error = new ValidationError(
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
                    error = new ValidationError("nonFieldErrors", ErrorCode.ExistingLock, "Het document is al gelockt.");
                    return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                }

                enkelvoudigInformatieObject.Locked = true;
                enkelvoudigInformatieObject.Lock = Guid.NewGuid().ToString().Replace("-", "");
            }
            else
            {
                if (request.Lock != null && enkelvoudigInformatieObject.Lock != request.Lock)
                {
                    error = new ValidationError("nonFieldErrors", ErrorCode.IncorrectLockId, "Incorrect lock ID.");
                    return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                }

                if (request.Lock == null)
                {
                    if (!AuthorizationContextAccessor.AuthorizationContext.IsForcedUnlockAuthorized())
                    {
                        error = new ValidationError("nonFieldErrors", ErrorCode.MissingLockId, "Dit is een verplicht veld.");
                        return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                    }
                }

                // Handle incompleted multi-part documents here if any

                bool multipleVersions = enkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.Count > 1;

                var current = enkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.OrderBy(e => e.Versie).Last();
                if (current.Inhoud == null && current.Bestandsomvang > 0)
                {
                    await AbortCurrentBestandsdelenUploadAsync(request, multipleVersions, current, cancellationToken);
                }

                enkelvoudigInformatieObject.Locked = false;
                enkelvoudigInformatieObject.Lock = null;
            }

            audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(enkelvoudigInformatieObject);

            await audittrail.PatchedAsync(enkelvoudigInformatieObject, enkelvoudigInformatieObject, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            if (request.Set)
                _logger.LogDebug(
                    "EnkelvoudigInformatieObject successfully locked. Lock={enkelvoudigInformatieObjectLock}",
                    enkelvoudigInformatieObject.Lock
                );
            else
                _logger.LogDebug("EnkelvoudigInformatieObject successfully unlocked.");
        }
        return new CommandResult<string>(enkelvoudigInformatieObject.Lock, CommandStatus.OK);
    }

    private async Task AbortCurrentBestandsdelenUploadAsync(
        LockEnkelvoudigInformatieObjectCommand request,
        bool multipleVersions,
        EnkelvoudigInformatieObjectVersie current,
        CancellationToken cancellationToken
    )
    {
        IMultiPartDocument multipartdocument = new MultiPartDocument(current.MultiPartDocumentId);

        if (multipleVersions)
        {
            // Yes: remove the incompleted version so the previous one will be the current!

            // 1. Cleanup underlying (temporary files) from document-storage of the unmerged document
            await AbortMultiPartUploadsFromDocumentStorage(multipartdocument, cancellationToken);

            // 2. Remove the latest vesion which is not completed (at unlock)
            _context.Remove(current);

            // Note: After SaveChangesAsync the previous version is now the latest
        }
        else
        {
            // No: Set the only document version we have to null (so it will become a meta-only document)

            // 1. Cleanup bestandsdelen en reset document versie meta-data
            var bestandsdelen = await _context
                .EnkelvoudigInformatieObjectVersies.Include(e => e.BestandsDelen)
                .Where(e => e.EnkelvoudigInformatieObjectId == request.Id)
                .SelectMany(e => e.BestandsDelen)
                .Select(e => e)
                .ToArrayAsync(cancellationToken);

            _context.BestandsDelen.RemoveRange(bestandsdelen);

            current.Inhoud = null;
            current.Bestandsomvang = 0;
            current.MultiPartDocumentId = null;

            // 2. Cleanup underlying (temporary files) from document-storage of the unmerged document
            await AbortMultiPartUploadsFromDocumentStorage(multipartdocument, cancellationToken);

            // Note: After SaveChangesAsync the current version (incompleted multi-part document) has become a meta-only document!!
        }
    }

    private async Task AbortMultiPartUploadsFromDocumentStorage(IMultiPartDocument multipartdocument, CancellationToken cancellationToken)
    {
        try
        {
            await _documentService.AbortMultipartUploadAsync(multipartdocument, cancellationToken);
        }
        catch (Exception ex)
        {
            // Only log the error so the Unlock can continue without any problems!
            _logger.LogError(ex, "Could not abort current pending multi-part upload");
        }
    }

    private static AuditTrailOptions AuditTrailOptions =>
        new AuditTrailOptions { Bron = ServiceRoleName.DRC, Resource = "enkelvoudiginformatieobject" };
}

public class LockEnkelvoudigInformatieObjectCommand : IRequest<CommandResult<string>>
{
    public bool Set { get; internal set; }
    public Guid Id { get; internal set; }
    public string Lock { get; internal set; }
}
