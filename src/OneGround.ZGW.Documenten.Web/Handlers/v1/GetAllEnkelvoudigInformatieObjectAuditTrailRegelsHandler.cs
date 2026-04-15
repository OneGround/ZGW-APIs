using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.AuditTrail;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler
    : DocumentenBaseHandler<GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler>,
        IRequestHandler<GetAllEnkelvoudigInformatieObjectAuditTrailRegels, QueryResult<IEnumerable<AuditTrailRegel>>>
{
    private readonly DrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler(
        ILogger<GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<QueryResult<IEnumerable<AuditTrailRegel>>> Handle(
        GetAllEnkelvoudigInformatieObjectAuditTrailRegels request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all audittrailregels enkelvoudigInformatieObject....");

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.AsNoTracking()
            .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(a => a.Id == request.EnkelvoudigInformatieObjectId, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(enkelvoudigInformatieObject))
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.Forbidden);
        }

        using var audittrail = _auditTrailFactory.Create(enkelvoudigInformatieObject.LegacyAuditTrail);

        var result = await audittrail.GetAuditTrailEntriesAsync(enkelvoudigInformatieObject.Id, cancellationToken);

        return new QueryResult<IEnumerable<AuditTrailRegel>>(result, QueryStatus.OK);
    }
}

class GetAllEnkelvoudigInformatieObjectAuditTrailRegels : IRequest<QueryResult<IEnumerable<AuditTrailRegel>>>
{
    public Guid EnkelvoudigInformatieObjectId { get; internal set; }
}
