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
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class GetZaakDeltasQueryHandler
    : ZakenBaseHandler<GetZaakDeltasQueryHandler>,
        IRequestHandler<GetZaakDeltasQuery, QueryResult<PagedResult<AuditTrailDelta>>>
{
    private const int DefaultLimit = 100;

    private readonly ZrcDbContext _context;

    public GetZaakDeltasQueryHandler(
        ILogger<GetZaakDeltasQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<AuditTrailDelta>>> Handle(GetZaakDeltasQuery request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        // Note: AuditTrailDelta draagt zelf geen Owner/RSIN-kolom; multi-tenant scoping gebeurt daarom
        // via een join naar Zaak op HoofdObjectId, analoog aan de authorization-join in GetAllZakenQueryHandler.
        // ResourceId == HoofdObjectId is bewust (i.p.v. Resource == "zaak"): audittrail_deltas logt ook
        // mutaties op sub-resources (rol, zaakobject, status, ...), die ieder hun eigen top-level
        // resource in de ZGW-API zijn (met eigen endpoints), geen onderdeel van de ZAAK-representatie
        // zelf. Bij een zaak-mutatie is ResourceId per definitie gelijk aan HoofdObjectId; bij een
        // sub-resource-mutatie wijkt ResourceId daarvan af — een structurele guid-vergelijking, niet
        // afhankelijk van een losse stringliteral die per schrijvende handler kan verschillen.
        var query = _context
            .AuditTrailDeltas.Where(d => AuditTrailSyncActies.Mutaties.Contains(d.Actie) && d.ResourceId == d.HoofdObjectId)
            .Join(_context.Zaken.Where(rsinFilter), delta => delta.HoofdObjectId, zaak => (Guid?)zaak.Id, (delta, zaak) => delta);

        // 'after' is verplicht (zie GetZaakDeltasQueryParameters) en dient een dubbel doel: het is
        // zowel het startpunt in de tijd áls de resource-identificatie. _deltas is het vervolg van de
        // versie-geschiedenis van díe ene zaak (dezelfde HoofdObjectId/ResourceId als de anchor-rij),
        // geen collectie-brede stream — vandaar het eerst opzoeken van de anchor, en dan pas scopen.
        var anchor = await query
            .Where(d => d.Id == request.After)
            .Select(d => new
            {
                d.AanmaakDatum,
                d.Id,
                d.HoofdObjectId,
                d.ResourceId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (anchor == null)
        {
            return new QueryResult<PagedResult<AuditTrailDelta>>(null, QueryStatus.NotFound);
        }

        var resourceQuery = query.Where(d => d.HoofdObjectId == anchor.HoofdObjectId && d.ResourceId == anchor.ResourceId);

        // Note: PoC — geen dedicated index op (hoofdobject_id, resource_id, aanmaakdatum, id) voor deze
        // delta/snapshot-sync query. Als dit qua performance een probleem wordt bij grote datasets,
        // overweeg een covering index analoog aan "t3b_IX_eio_owner_creationtime_id_incl_type_vha" in
        // OneGround.ZGW.Documenten.Web/Handlers/v1/7/GetAllEnkelvoudigInformatieObjectenQueryHandler.cs
        // (keyset-paginatie op (AanmaakDatum DESC, Id ASC)).
        var orderedQuery = resourceQuery.OrderBy(d => d.AanmaakDatum).ThenBy(d => d.Id);

        var totalCount = await orderedQuery.CountAsync(cancellationToken);

        // Keyset-seek: alles wat strikt ná de anchor komt in dezelfde (AanmaakDatum, Id)-ordening
        // (vgl. de keyset-seek in GetAllEnkelvoudigInformatieObjectenQueryHandler hierboven, al
        // ontbreekt hier nog de index die dat sargable zou maken).
        var filteredQuery = orderedQuery.Where(d =>
            d.AanmaakDatum > anchor.AanmaakDatum || (d.AanmaakDatum == anchor.AanmaakDatum && d.Id > anchor.Id)
        );

        var pageResult = await filteredQuery.Take(request.Limit ?? DefaultLimit).ToListAsync(cancellationToken);

        var result = new PagedResult<AuditTrailDelta> { PageResult = pageResult, Count = totalCount };

        return new QueryResult<PagedResult<AuditTrailDelta>>(result, QueryStatus.OK);
    }
}

class GetZaakDeltasQuery : IRequest<QueryResult<PagedResult<AuditTrailDelta>>>
{
    public Guid? After { get; internal init; }
    public int? Limit { get; internal init; }
}
