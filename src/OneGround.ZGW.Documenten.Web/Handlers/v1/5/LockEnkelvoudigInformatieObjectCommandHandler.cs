using System;
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
using OneGround.ZGW.Documenten.Contracts.v1._5.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Services;
using OneGround.ZGW.Documenten.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

public class LockEnkelvoudigInformatieObjectCommandHandler
    : DocumentenBaseHandler<LockEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<LockEnkelvoudigInformatieObjectCommand, CommandResult<string>>
{
    private readonly DrcDbContext _context;
    private readonly DrcDbContext2 _context2;
    private readonly IAuditTrailFactory _auditTrailFactory;
    private readonly IDocumentService _documentService;

    public LockEnkelvoudigInformatieObjectCommandHandler(
        ILogger<LockEnkelvoudigInformatieObjectCommandHandler> logger,
        IConfiguration configuration,
        DrcDbContext context,
        DrcDbContext2 context2,
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
        _context2 = context2;
        _auditTrailFactory = auditTrailFactory;

        _documentService = documentServicesResolver.GetDefault();
    }

    public async Task<CommandResult<string>> Handle(LockEnkelvoudigInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        if (request.Set)
            _logger.LogDebug("Locking EnkelvoudigInformatieObject....");
        else
            _logger.LogDebug("Unlocking EnkelvoudigInformatieObject....");

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObjectLock2>();

        var enkelvoudigInformatieObjectLock = await _context2
            .EnkelvoudigInformatieObjectLocks.Where(rsinFilter)
            .Where(l => l.EnkelvoudigInformatieObjecten.Any(e => e.EnkelvoudigInformatieObjectId == request.Id))
            .Include(e => e.EnkelvoudigInformatieObjecten)
            .SingleOrDefaultAsync(cancellationToken);

        if (enkelvoudigInformatieObjectLock == null)
        {
            var error = new ValidationError("id", ErrorCode.NotFound, $"EnkelvoudigInformatieObject {request.Id} is onbekend.");

            return new CommandResult<string>(null, CommandStatus.NotFound, error);
        }

        using (var audittrail = _auditTrailFactory.Create(AuditTrailOptions))
        {
            //audittrail.SetOld<EnkelvoudigInformatieObjectGetResponseDto>(enkelvoudigInformatieObjectLock);

            if (request.Set)
            {
                if (enkelvoudigInformatieObjectLock.Locked)
                {
                    var error = new ValidationError("nonFieldErrors", ErrorCode.ExistingLock, "Het document is al gelockt.");
                    return new CommandResult<string>(null, CommandStatus.ValidationError, error);
                }

                enkelvoudigInformatieObjectLock.Locked = true;
                enkelvoudigInformatieObjectLock.Lock = Guid.NewGuid().ToString().Replace("-", "");
            }
            else
            {
                if (request.Lock != null && enkelvoudigInformatieObjectLock.Lock != request.Lock)
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

                // Handle incompleted multi-part documents here if any

                bool multipleVersions = enkelvoudigInformatieObjectLock.EnkelvoudigInformatieObjecten.Count > 1;

                var current = enkelvoudigInformatieObjectLock.EnkelvoudigInformatieObjecten.OrderBy(e => e.Versie).Last();
                if (current.Inhoud == null && current.Bestandsomvang > 0)
                {
                    await AbortCurrentBestandsdelenUploadAsync(request, multipleVersions, current, cancellationToken);
                }

                enkelvoudigInformatieObjectLock.Locked = false;
                enkelvoudigInformatieObjectLock.Lock = null;
            }

            //audittrail.SetNew<EnkelvoudigInformatieObjectGetResponseDto>(enkelvoudigInformatieObjectLock);

            await audittrail.PatchedAsync(enkelvoudigInformatieObjectLock, enkelvoudigInformatieObjectLock, cancellationToken);

            await _context2.SaveChangesAsync(cancellationToken);

            if (request.Set)
                _logger.LogDebug(
                    "EnkelvoudigInformatieObject successfully locked. Lock={enkelvoudigInformatieObjectLock}",
                    enkelvoudigInformatieObjectLock.Lock
                );
            else
                _logger.LogDebug("EnkelvoudigInformatieObject successfully unlocked.");
        }
        return new CommandResult<string>(enkelvoudigInformatieObjectLock.Lock, CommandStatus.OK);
    }

    private async Task AbortCurrentBestandsdelenUploadAsync(
        LockEnkelvoudigInformatieObjectCommand request,
        bool multipleVersions,
        EnkelvoudigInformatieObject2 current,
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
