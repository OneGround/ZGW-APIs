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
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetAllZaakAuditTrailRegelsHandler
    : ZakenBaseHandler<GetAllZaakAuditTrailRegelsHandler>,
        IRequestHandler<GetAllZaakAuditTrailRegels, QueryResult<IEnumerable<AuditTrailRegel>>>
{
    private readonly ZrcDbContext _context;
    private readonly IAuditTrailFactory _auditTrailFactory;

    public GetAllZaakAuditTrailRegelsHandler(
        ILogger<GetAllZaakAuditTrailRegelsHandler> logger,
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

        using var audittrail = _auditTrailFactory.Create(zaak.LegacyAuditTrail);

        var result = await audittrail.GetAuditTrailEntriesAsync(zaak.Id, cancellationToken);

        return new QueryResult<IEnumerable<AuditTrailRegel>>(result, QueryStatus.OK);
    }
}

class GetAllZaakAuditTrailRegels : IRequest<QueryResult<IEnumerable<AuditTrailRegel>>>
{
    public Guid ZaakId { get; set; }
}
