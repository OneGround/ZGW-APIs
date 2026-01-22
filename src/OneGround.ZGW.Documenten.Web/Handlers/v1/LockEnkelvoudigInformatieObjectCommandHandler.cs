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
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class LockEnkelvoudigInformatieObjectCommandHandler
    : DocumentenBaseHandler<LockEnkelvoudigInformatieObjectCommandHandler>,
        IRequestHandler<LockEnkelvoudigInformatieObjectCommand, CommandResult<string>>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public LockEnkelvoudigInformatieObjectCommandHandler(
        ILogger<LockEnkelvoudigInformatieObjectCommandHandler> logger,
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

    public async Task<CommandResult<string>> Handle(LockEnkelvoudigInformatieObjectCommand request, CancellationToken cancellationToken)
    {
        if (request.Set)
            _logger.LogDebug("Locking EnkelvoudigInformatieObject....");
        else
            _logger.LogDebug("Unlocking EnkelvoudigInformatieObject....");

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.Where(rsinFilter)
            .Include(e => e.EnkelvoudigInformatieObjectVersies)
            .SingleOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            var error = new ValidationError("id", ErrorCode.NotFound, $"EnkelvoudigInformatieObject {request.Id} is onbekend.");

            return new CommandResult<string>(null, CommandStatus.NotFound, error);
        }

        // FUND-1595 latest_enkelvoudiginformatieobjectversie_id [FK] NULL seen on PROD only
        if (enkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersie == null)
        {
            // Not very elegant but it's a temporary work around until we figure out the problem.
            var latestVersion = enkelvoudigInformatieObject.EnkelvoudigInformatieObjectVersies.OrderByDescending(e => e.Versie).First();
            enkelvoudigInformatieObject.LatestEnkelvoudigInformatieObjectVersie = latestVersion;

            _logger.LogWarning("LatestEnkelvoudigInformatieObjectVersie is NULL -> restored");
        }
        // ----

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
