using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetAllZakenQueryHandler : ZakenBaseHandler<GetAllZakenQueryHandler>, IRequestHandler<GetAllZakenQuery, QueryResult<PagedResult<Zaak>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZakenQueryHandler(
        ILogger<GetAllZakenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<Zaak>>> Handle(GetAllZakenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Get all Zaken....");

        if (request.WithinZaakGeometry != null)
        {
            request.WithinZaakGeometry.SRID = request.SRID;
        }

        var filter = GetZaakFilterPredicate(request.GetAllZakenFilter);
        var geometryFilter = GetZaakGeometryFilterPredicate(request.WithinZaakGeometry, request.SRID);
        var ordering = GetZaakOrdering(request.Ordering);
        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var query = _context.Zaken.AsNoTracking().Where(rsinFilter).Where(filter).Where(geometryFilter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaaktype, i => i.ZaakType, (z, a) => new { Zaak = z, Authorisatie = a })
                .Where(z => (int)z.Zaak.VertrouwelijkheidAanduiding <= z.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(z => z.Zaak);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(z => z.Hoofdzaak)
            .Include(z => z.Deelzaken)
            .Include(z => z.RelevanteAndereZaken)
            .Include(z => z.Kenmerken)
            .Include(z => z.ZaakEigenschappen)
            .Include(z => z.ZaakStatussen)
            .Include(z => z.Resultaat)
            .Include(z => z.Verlenging)
            .Include(z => z.Opschorting)
            .OrderBy(ordering)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .Select(z => new
            {
                Zaak = z,
                ZaakgeometrieConverted = z.Zaakgeometrie != null && request.SRID != 28992
                    ? EF.Functions.Transform(z.Zaakgeometrie, request.SRID)
                    : null,
            })
            .ToListAsync(cancellationToken);

        if (request.SRID != 28992)
        {
            pagedResult.Where(z => z.Zaak.Zaakgeometrie != null).ToList().ForEach(z => z.Zaak.Zaakgeometrie = z.ZaakgeometrieConverted);
        }

        var result = new PagedResult<Zaak> { PageResult = pagedResult.Select(z => z.Zaak), Count = totalCount };

        return new QueryResult<PagedResult<Zaak>>(result, QueryStatus.OK);
    }

    private static Expression<Func<Zaak, object>> GetZaakOrdering(string order)
    {
        return order switch
        {
            "identificatie" => z => z.Identificatie,
            "bronorganisatie" => z => z.Bronorganisatie,
            "zaaktype" => z => z.Zaaktype,
            "registratiedatum" => z => z.Registratiedatum,
            "verantwoordelijkeorganisatie" => z => z.VerantwoordelijkeOrganisatie,
            "startdatum" => z => z.Startdatum,
            "einddatum" => z => z.Einddatum,
            "einddatumgepland" => z => z.EinddatumGepland,
            "archiefnominatie" => z => z.Archiefnominatie,
            "archiefstatus" => z => z.Archiefstatus,
            _ => z => z.Startdatum,
        };
    }

    private static Expression<Func<Zaak, bool>> GetZaakGeometryFilterPredicate(Geometry zaakGeometry, int srid)
    {
        if (srid == 28992) // Note: In the database all geometrie is stored in SRID 28992 (RDS)
            return z => zaakGeometry == null || z.Zaakgeometrie.Within(zaakGeometry);

        // ... so we have to convert to SRID 28992 otherwise
        return z => zaakGeometry == null || EF.Functions.Transform(z.Zaakgeometrie, srid).Within(zaakGeometry);
    }

    private static Expression<Func<Zaak, bool>> GetZaakFilterPredicate(GetAllZakenFilter filter)
    {
        return z =>
            (filter.Startdatum == null || z.Startdatum == filter.Startdatum)
            && (filter.Startdatum__gt == null || z.Startdatum > filter.Startdatum__gt)
            && (filter.Startdatum__gte == null || z.Startdatum >= filter.Startdatum__gte)
            && (filter.Startdatum__lt == null || z.Startdatum < filter.Startdatum__lt)
            && (filter.Startdatum__lte == null || z.Startdatum <= filter.Startdatum__lte)
            && (filter.Archiefstatus == null || z.Archiefstatus == filter.Archiefstatus)
            && (!filter.Archiefstatus__in.Any() || filter.Archiefstatus__in.Contains(z.Archiefstatus))
            && (filter.Identificatie == null || z.Identificatie == filter.Identificatie)
            && (filter.Bronorganisatie == null || z.Bronorganisatie == filter.Bronorganisatie)
            && (filter.Archiefnominatie == null || z.Archiefnominatie == filter.Archiefnominatie)
            && (!filter.Archiefnominatie__in.Any() || z.Archiefnominatie != null && filter.Archiefnominatie__in.Contains(z.Archiefnominatie.Value))
            && (filter.Archiefactiedatum == null || z.Archiefactiedatum != null && z.Archiefactiedatum == filter.Archiefactiedatum)
            && (filter.Archiefactiedatum__gt == null || z.Archiefactiedatum != null && z.Archiefactiedatum > filter.Archiefactiedatum__gt)
            && (filter.Archiefactiedatum__lt == null || z.Archiefactiedatum != null && z.Archiefactiedatum < filter.Archiefactiedatum__lt)
            && (filter.Zaaktype == null || z.Zaaktype == filter.Zaaktype);
    }
}

public class GetAllZakenQuery : IRequest<QueryResult<PagedResult<Zaak>>>
{
    public GetAllZakenFilter GetAllZakenFilter { get; internal set; }
    public Geometry WithinZaakGeometry { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
    public string Ordering { get; internal set; }
    public int SRID { get; internal set; } = 28992;
}
