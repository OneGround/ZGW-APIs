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
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class GetZaakAuditTrailRegelHandler
    : ZakenBaseHandler<GetZaakAuditTrailRegelHandler>,
        IRequestHandler<GetZaakAuditTrailRegel, QueryResult<AuditTrailRegel>>
{
    private readonly ZrcDbContext _context;

    public GetZaakAuditTrailRegelHandler(
        ILogger<GetZaakAuditTrailRegelHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<AuditTrailRegel>> Handle(GetZaakAuditTrailRegel request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all audittrailregels zaak....");

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context.Zaken.AsNoTracking().Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == request.ZaakId, cancellationToken);

        if (zaak == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.Forbidden);
        }

        var result = await _context
            .AuditTrailRegels.AsNoTracking()
            .Where(a => a.Id == request.AuditTrailRegelId)
            .Where(a => a.HoofdObjectId.HasValue && a.HoofdObjectId == zaak.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new QueryResult<AuditTrailRegel>(null, QueryStatus.NotFound);
        }

        return new QueryResult<AuditTrailRegel>(result, QueryStatus.OK);
    }
}

class GetZaakAuditTrailRegel : IRequest<QueryResult<AuditTrailRegel>>
{
    public Guid ZaakId { get; set; }
    public Guid AuditTrailRegelId { get; set; }
}
