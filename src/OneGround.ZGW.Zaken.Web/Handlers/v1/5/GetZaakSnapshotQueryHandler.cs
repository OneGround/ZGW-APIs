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
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class GetZaakSnapshotQueryHandler : ZakenBaseHandler<GetZaakSnapshotQueryHandler>, IRequestHandler<GetZaakSnapshotQuery, QueryResult<AuditTrailDelta>>
{
    private readonly ZrcDbContext _context;

    public GetZaakSnapshotQueryHandler(
        ILogger<GetZaakSnapshotQueryHandler> logger,
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IEntityUriService uriService,
        IZaakKenmerkenResolver zaakKenmerkenResolver,
        ZrcDbContext context
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
    }

    public async Task<QueryResult<AuditTrailDelta>> Handle(GetZaakSnapshotQuery request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        // Note: AuditTrailDelta draagt zelf geen Owner/RSIN-kolom; multi-tenant scoping gebeurt daarom
        // via een join naar Zaak op HoofdObjectId, net als in GetZaakSnapshotsQueryHandler. SnapshotJson
        // != null onderscheidt een echte snapshot-rij van een delta_json-only rij — een delta-id hoort
        // hier niet te resolven, ook al deelt het dezelfde guid-namespace. ResourceId == HoofdObjectId
        // sluit sub-resource-snapshots (rol, zaakobject, ...) uit, consistent met GetZaakSnapshotsQueryHandler
        // en GetZaakDeltasQueryHandler.
        var snapshot = await _context
            .AuditTrailDeltas.Where(d =>
                AuditTrailSyncActies.Mutaties.Contains(d.Actie)
                && d.SnapshotJson != null /*&& d.ResourceId == d.HoofdObjectId*/
                && d.Id == request.Id
            )
            .Join(_context.Zaken.Where(rsinFilter), delta => delta.HoofdObjectId, zaak => (Guid?)zaak.Id, (delta, zaak) => delta)
            .SingleOrDefaultAsync(cancellationToken);

        if (snapshot == null)
        {
            return new QueryResult<AuditTrailDelta>(null, QueryStatus.NotFound);
        }

        return new QueryResult<AuditTrailDelta>(snapshot, QueryStatus.OK);
    }
}

class GetZaakSnapshotQuery : IRequest<QueryResult<AuditTrailDelta>>
{
    public Guid Id { get; internal init; }
}
