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
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class GetZaakSnapshotsQueryHandler
    : ZakenBaseHandler<GetZaakSnapshotsQueryHandler>,
        IRequestHandler<GetZaakSnapshotsQuery, QueryResult<List<ZaakSnapshotResult>>>
{
    private const int DefaultLimit = 100;

    private readonly ZrcDbContext _context;

    public GetZaakSnapshotsQueryHandler(
        ILogger<GetZaakSnapshotsQueryHandler> logger,
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

    public async Task<QueryResult<List<ZaakSnapshotResult>>> Handle(GetZaakSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        // Note: AuditTrailDelta draagt zelf geen Owner/RSIN-kolom; multi-tenant scoping gebeurt daarom
        // via een join naar Zaak op HoofdObjectId, net als in GetZaakDeltasQueryHandler.
        // ResourceId == HoofdObjectId is bewust (i.p.v. Resource == "zaak"): audittrail_deltas logt ook
        // mutaties op sub-resources (rol, zaakobject, status, ...), die ieder hun eigen top-level
        // resource in de ZGW-API zijn, geen onderdeel van de ZAAK-representatie zelf. Zonder deze
        // filter zou _snapshots items kunnen aanbieden die niet bruikbaar zijn als anker bij
        // _deltas (die diezelfde filter al afdwingt) — elke rij hier moet een geldig vervolgpunt zijn.
        var mutatieDeltasQuery = _context
            .AuditTrailDeltas.Where(d =>
                AuditTrailSyncActies.Mutaties.Contains(d.Actie) /*&& d.ResourceId == d.HoofdObjectId*/
            )
            .Join(_context.Zaken.Where(rsinFilter), delta => delta.HoofdObjectId, zaak => (Guid?)zaak.Id, (delta, zaak) => delta);

        // Elke rij met een SnapshotJson is een losse, volledige-state-vastlegging van één resource
        // (de zaak zelf, of een sub-resource zoals zaakobject/rol/...) op een specifieke versie —
        // dat is de daadwerkelijke "snapshot" die deze endpoint aanbiedt. De id is de echte primary
        // key van die audittrail-rij (geen berekende positie), zodat hij direct en stabiel aansluit
        // op _deltas: ?after=<dit-id> vraagt precies alles op ná deze specifieke rij.
        var snapshotRowsQuery = mutatieDeltasQuery.Where(d => d.SnapshotJson != null);

        if (request.AanmaakdatumGte.HasValue)
        {
            // AanmaakDatum is 'timestamp with time zone'; Npgsql accepteert daarvoor alleen DateTime met
            // Kind=Utc, terwijl de query-string-binding van AanmaakdatumGte Kind=Unspecified oplevert.
            var aanmaakdatumGte = DateTime.SpecifyKind(request.AanmaakdatumGte.Value, DateTimeKind.Utc);
            snapshotRowsQuery = snapshotRowsQuery.Where(d => d.AanmaakDatum >= aanmaakdatumGte);
        }

        // Chronologisch (AanmaakDatum, Id) — zelfde als _deltas. Sorteren op HoofdObjectId/ResourceId
        // (guids, niet chronologisch) zou betekenen dat een nieuwe mutatie vóór een al-gepagineerde
        // cursor kan belanden en dus nooit meer gezien wordt door een pollende consument.
        var orderedSnapshotQuery = snapshotRowsQuery.OrderBy(d => d.AanmaakDatum).ThenBy(d => d.Id);

        // Het aantal snapshots kan te groot zijn om in één keer op te halen, dus ook _snapshots
        // ondersteunt 'after' (id van een eerder teruggegeven snapshot-item) en 'limit' via een
        // keyset-seek, zelfde patroon als GetZaakDeltasQueryHandler. Een onbekende/ongeldige 'after'
        // wordt niet als fout behandeld; de pagina start dan gewoon vanaf het begin van de reeks.
        IQueryable<AuditTrailDelta> filteredQuery = orderedSnapshotQuery;

        if (request.After.HasValue)
        {
            var anchor = await mutatieDeltasQuery
                .Where(d => d.Id == request.After.Value)
                .Select(d => new { d.AanmaakDatum, d.Id })
                .SingleOrDefaultAsync(cancellationToken);

            if (anchor != null)
            {
                filteredQuery = orderedSnapshotQuery.Where(d =>
                    d.AanmaakDatum > anchor.AanmaakDatum || (d.AanmaakDatum == anchor.AanmaakDatum && d.Id > anchor.Id)
                );
            }
        }

        var pageRows = await filteredQuery.Take(request.Limit ?? DefaultLimit).ToListAsync(cancellationToken);

        // Note: PoC — 'versie' loopt per resource (HoofdObjectId + Resource + ResourceId), niet
        // globaal (zie AuditTrailSyncActies/GetZaakDeltasQueryHandler); 'total' is het aantal
        // delta_json-rijen van deze resource tót de volgende snapshot van diezelfde resource, of het
        // hoogst bereikte versienummer als er nog geen volgende snapshot is. Dit vraagt 1-2 gerichte
        // subqueries per rij in de pagina (dus begrensd door Limit, niet door de totale audittrail-
        // omvang) — geen index op (hoofdobject_id, resource, resource_id, versie) voor deze subqueries,
        // zie ook de Note in GetZaakDeltasQueryHandler over de ontbrekende covering index.
        var snapshots = new List<ZaakSnapshotResult>();

        foreach (var row in pageRows)
        {
            var perResourceQuery = mutatieDeltasQuery.Where(d =>
                d.HoofdObjectId == row.HoofdObjectId && d.Resource == row.Resource && d.ResourceId == row.ResourceId
            );

            var nextSnapshotVersie = await perResourceQuery
                .Where(d => d.Versie > row.Versie && d.SnapshotJson != null)
                .OrderBy(d => d.Versie)
                .Select(d => (int?)d.Versie)
                .FirstOrDefaultAsync(cancellationToken);

            var total = nextSnapshotVersie.HasValue
                ? nextSnapshotVersie.Value - row.Versie - 1
                : await perResourceQuery.MaxAsync(d => d.Versie, cancellationToken) - row.Versie;

            snapshots.Add(
                new ZaakSnapshotResult
                {
                    Id = row.Id.ToString(),
                    Href = _uriService.GetUri("zaken", "_snapshots", row.Id.ToString()),
                    Resource = row.Resource,
                    ResourceId = row.ResourceId?.ToString(),
                    Total = total,
                }
            );
        }

        return new QueryResult<List<ZaakSnapshotResult>>(snapshots, QueryStatus.OK);
    }
}

class GetZaakSnapshotsQuery : IRequest<QueryResult<List<ZaakSnapshotResult>>>
{
    public Guid? After { get; internal init; }
    public int? Limit { get; internal init; }
    public DateTime? AanmaakdatumGte { get; internal init; }
}

class ZaakSnapshotResult
{
    public string Id { get; init; }
    public string Href { get; init; }
    public string Resource { get; init; }
    public string ResourceId { get; init; }
    public int Total { get; init; }
}
