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
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class GetAllZaakAuditTrailRegelsHandler
    : ZakenBaseHandler<GetAllZaakAuditTrailRegelsHandler>,
        IRequestHandler<GetAllZaakAuditTrailRegels, QueryResult<IEnumerable<AuditTrailRegel>>>
{
    private readonly ZrcDbContext _context;

    public GetAllZaakAuditTrailRegelsHandler(
        ILogger<GetAllZaakAuditTrailRegelsHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<IEnumerable<AuditTrailRegel>>> Handle(GetAllZaakAuditTrailRegels request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all audittrailregels zaak....");

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context.Zaken.AsNoTracking().Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == request.ZaakId, cancellationToken);

        if (zaak == null)
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new QueryResult<IEnumerable<AuditTrailRegel>>(null, QueryStatus.Forbidden);
        }

        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == zaak.Id)
            .OrderBy(a => a.AanmaakDatum)
            .ToListAsync(cancellationToken);

        return new QueryResult<IEnumerable<AuditTrailRegel>>(result, QueryStatus.OK);
    }
}

class GetAllZaakAuditTrailRegels : IRequest<QueryResult<IEnumerable<AuditTrailRegel>>>
{
    public Guid ZaakId { get; set; }
}
