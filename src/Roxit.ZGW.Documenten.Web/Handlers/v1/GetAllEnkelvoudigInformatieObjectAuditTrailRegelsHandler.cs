using System;
using System.Collections.Generic;
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

class GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler
    : DocumentenBaseHandler<GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler>,
        IRequestHandler<GetAllEnkelvoudigInformatieObjectAuditTrailRegels, QueryResult<IEnumerable<AuditTrailRegel>>>
{
    private readonly DrcDbContext _context;

    public GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler(
        ILogger<GetAllEnkelvoudigInformatieObjectAuditTrailRegelsHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
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
            .Where(rsinFilter)
            .SingleOrDefaultAsync(a => a.Id == request.EnkelvoudigInformatieObjectId, cancellationToken);

        if (enkelvoudigInformatieObject == null)
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.NotFound);
        }

        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == enkelvoudigInformatieObject.Id)
            .OrderBy(a => a.AanmaakDatum)
            .ToListAsync(cancellationToken);

        return new QueryResult<IEnumerable<AuditTrailRegel>>(result, QueryStatus.OK);
    }
}

class GetAllEnkelvoudigInformatieObjectAuditTrailRegels : IRequest<QueryResult<IEnumerable<AuditTrailRegel>>>
{
    public Guid EnkelvoudigInformatieObjectId { get; internal set; }
}
