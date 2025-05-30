using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.Authorization;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess.AuditTrail;

namespace Roxit.ZGW.Besluiten.Web.Handlers.v1;

class GetBesluitAuditTrailRegelHandler
    : BesluitenBaseHandler<GetBesluitAuditTrailRegelHandler>,
        IRequestHandler<GetBesluitAuditTrailRegel, QueryResult<AuditTrailRegel>>
{
    private readonly BrcDbContext _context;

    public GetBesluitAuditTrailRegelHandler(
        ILogger<GetBesluitAuditTrailRegelHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
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

        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .Where(a => a.Id == request.AuditTrailRegelId)
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == besluit.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        var queryResult = new QueryResult<AuditTrailRegel>(result, QueryStatus.OK);

        return queryResult;
    }
}

class GetBesluitAuditTrailRegel : IRequest<QueryResult<AuditTrailRegel>>
{
    public Guid BesluitId { get; internal set; }
    public Guid AuditTrailRegelId { get; set; }
}
