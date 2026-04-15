using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Authorization;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class GetBesluitAuditTrailRegelHandler
    : BesluitenBaseHandler<GetBesluitAuditTrailRegelHandler>,
        IRequestHandler<GetBesluitAuditTrailRegel, QueryResult<AuditTrailRegel>>
{
    private readonly BrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public GetBesluitAuditTrailRegelHandler(
        ILogger<GetBesluitAuditTrailRegelHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, besluitKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<QueryResult<AuditTrailRegel>> Handle(GetBesluitAuditTrailRegel request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get audittrailregel besluit....");

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context
            .Besluiten.AsNoTracking()
            .Where(rsinFilter)
            .SingleOrDefaultAsync(b => b.Id == request.BesluitId, cancellationToken);

        if (besluit == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(besluit))
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.Forbidden);
        }

        using var audittrail = _auditTrailFactory.Create(besluit.LegacyAuditTrail);

        var result = await audittrail.GetAuditTrailEntryByIdAsync(request.BesluitId, request.AuditTrailRegelId, cancellationToken);
        if (result == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        return new QueryResult<AuditTrailRegel>(result, QueryStatus.OK);
    }
}

class GetBesluitAuditTrailRegel : IRequest<QueryResult<AuditTrailRegel>>
{
    public Guid BesluitId { get; internal set; }
    public Guid AuditTrailRegelId { get; set; }
}
