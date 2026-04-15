using System;
using System.Collections.Generic;
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

class GetAllBesluitAuditTrailRegelsHandler
    : BesluitenBaseHandler<GetAllBesluitAuditTrailRegelsHandler>,
        IRequestHandler<GetAllBesluitAuditTrailRegels, QueryResult<IEnumerable<AuditTrailRegel>>>
{
    private readonly BrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public GetAllBesluitAuditTrailRegelsHandler(
        ILogger<GetAllBesluitAuditTrailRegelsHandler> logger,
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

    public async Task<QueryResult<IEnumerable<AuditTrailRegel>>> Handle(GetAllBesluitAuditTrailRegels request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all audittrailregels besluit....");

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context
            .Besluiten.AsNoTracking()
            .Where(rsinFilter)
            .SingleOrDefaultAsync(b => b.Id == request.BesluitId, cancellationToken);

        if (besluit == null)
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(besluit))
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.Forbidden);
        }

        using var audittrail = _auditTrailFactory.Create(besluit.LegacyAuditTrail);

        var result = await audittrail.GetAuditTrailEntriesAsync(besluit.Id, cancellationToken);

        return new QueryResult<IEnumerable<AuditTrailRegel>>(result, QueryStatus.OK);
    }
}

class GetAllBesluitAuditTrailRegels : IRequest<QueryResult<IEnumerable<AuditTrailRegel>>>
{
    public Guid BesluitId { get; internal set; }
}
