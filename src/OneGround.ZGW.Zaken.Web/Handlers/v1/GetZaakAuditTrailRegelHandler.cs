using System;
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
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetZaakAuditTrailRegelHandler
    : ZakenBaseHandler<GetZaakAuditTrailRegelHandler>,
        IRequestHandler<GetZaakAuditTrailRegel, QueryResult<AuditTrailRegel>>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public GetZaakAuditTrailRegelHandler(
        ILogger<GetZaakAuditTrailRegelHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver,
        IAuditTrailFactory auditTrailFactory
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _auditTrailFactory = auditTrailFactory;
    }

    public async Task<QueryResult<AuditTrailRegel>> Handle(GetZaakAuditTrailRegel request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get audittrailregel zaak....");

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

        using var audittrail = _auditTrailFactory.Create(zaak.LegacyAuditTrail);

        var result = await audittrail.GetAuditTrailEntryByIdAsync(request.ZaakId, request.AuditTrailRegelId, cancellationToken);
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
