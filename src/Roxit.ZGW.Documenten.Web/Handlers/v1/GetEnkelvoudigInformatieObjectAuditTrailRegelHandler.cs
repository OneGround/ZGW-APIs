using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess.AuditTrail;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

class GetEnkelvoudigInformatieObjectAuditTrailRegelHandler
    : DocumentenBaseHandler<GetEnkelvoudigInformatieObjectAuditTrailRegelHandler>,
        IRequestHandler<GetEnkelvoudigInformatieObjectAuditTrailRegel, QueryResult<AuditTrailRegel>>
{
    private readonly DrcDbContext _context;

    public GetEnkelvoudigInformatieObjectAuditTrailRegelHandler(
        ILogger<GetEnkelvoudigInformatieObjectAuditTrailRegelHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<AuditTrailRegel>> Handle(GetEnkelvoudigInformatieObjectAuditTrailRegel request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get audittrailregel enkelvoudigInformatieObject....");

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var enkelvoudigInformatieObject = await _context
            .EnkelvoudigInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .SingleOrDefaultAsync(a => a.Id == request.EnkelvoudigInformatieObjectId, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .Where(a => a.Id == request.AuditTrailRegelId)
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == enkelvoudigInformatieObject.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        return new QueryResult<AuditTrailRegel>(result, QueryStatus.OK);
    }
}

class GetEnkelvoudigInformatieObjectAuditTrailRegel : IRequest<QueryResult<AuditTrailRegel>>
{
    public Guid EnkelvoudigInformatieObjectId { get; internal set; }
    public Guid AuditTrailRegelId { get; internal set; }
}
