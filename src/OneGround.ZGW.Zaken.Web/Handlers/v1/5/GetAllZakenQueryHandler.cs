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
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class GetAllZakenQueryHandler : ZakenBaseHandler<GetAllZakenQueryHandler>, IRequestHandler<GetAllZakenQuery, QueryResult<PagedResult<Zaak>>>
{
    private readonly ZrcDbContext _context;
    private readonly IDistributedCacheHelper _cache;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZakenQueryHandler(
        ILogger<GetAllZakenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDistributedCacheHelper cache,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
        _cache = cache;
    }

    public async Task<QueryResult<PagedResult<Zaak>>> Handle(GetAllZakenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Zaken....");

        if (request.WithinZaakGeometry != null)
        {
            request.WithinZaakGeometry.SRID = request.SRID;
        }

        var geometryFilter = GetZaakGeometryFilterPredicate(request.WithinZaakGeometry, request.SRID);
        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var query = _context.Zaken.AsSplitQuery().AsNoTracking().Where(rsinFilter).Where(request.GetAllZakenFilter).Where(geometryFilter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaaktype, i => i.ZaakType, (z, a) => new { Zaak = z, Authorisatie = a })
                .Where(z => (int)z.Zaak.VertrouwelijkheidAanduiding <= z.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(z => z.Zaak);
        }

        var totalCount = await GetTotalCountCachedAsync(query, request.GetAllZakenFilter, request.WithinZaakGeometry, cancellationToken);

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
            .Include(z => z.Processobject)
            .Include(z => z.ZaakRollen)
            .Include(z => z.ZaakInformatieObjecten)
            .Include(z => z.ZaakObjecten)
            .OrderBy(request.Ordering)
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

    private static Expression<Func<Zaak, bool>> GetZaakGeometryFilterPredicate(Geometry zaakGeometry, int srid)
    {
        if (srid == 28992) // Note: In the database all geometrie is stored in SRID 28992 (RDS)
            return z => zaakGeometry == null || z.Zaakgeometrie.Within(zaakGeometry);

        // ... so we have to convert to SRID 28992 otherwise
        return z => zaakGeometry == null || EF.Functions.Transform(z.Zaakgeometrie, srid).Within(zaakGeometry);
    }

    private async Task<int> GetTotalCountCachedAsync(
        IQueryable<Zaak> query,
        GetAllZakenFilter filter,
        Geometry geometrie,
        CancellationToken cancellationToken
    )
    {
        // Create a key for the current request+ClientId (uri contains the query-parameters/geo as well)
        var key = ObjectHasher.ComputeSha1Hash(
            new
            {
                ClientId = _rsin,
                GetAllZakenFilter = filter,
                Geometrie = geometrie?.ToString(),
            }
        );

        // Note: Cache the Count from SQL for 1 minute
        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                var result = await query.CountAsync(cancellationToken);

                return result;
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(1),
            cancellationToken
        );

        return totalCount;
    }
}

internal static class IQueryableExtension
{
    public static IQueryable<Zaak> Where(this IQueryable<Zaak> zaken, GetAllZakenFilter filter)
    {
        return zaken
            .WhereIf(filter.Startdatum != null, z => z.Startdatum == filter.Startdatum)
            .WhereIf(filter.Startdatum__gt != null, z => z.Startdatum > filter.Startdatum__gt)
            .WhereIf(filter.Startdatum__gte != null, z => z.Startdatum >= filter.Startdatum__gte)
            .WhereIf(filter.Startdatum__lt != null, z => z.Startdatum < filter.Startdatum__lt)
            .WhereIf(filter.Startdatum__lte != null, z => z.Startdatum <= filter.Startdatum__lte)
            .WhereIf(filter.Archiefstatus != null, z => z.Archiefstatus == filter.Archiefstatus)
            .WhereIf(filter.Archiefstatus__in.Any(), z => filter.Archiefstatus__in.Contains(z.Archiefstatus))
            .WhereIf(filter.Identificatie != null, z => z.Identificatie == filter.Identificatie)
            .WhereIf(filter.Bronorganisatie != null, z => z.Bronorganisatie == filter.Bronorganisatie)
            .WhereIf(filter.Archiefnominatie != null, z => z.Archiefnominatie == filter.Archiefnominatie)
            .WhereIf(filter.Archiefnominatie__in.Any(), z => filter.Archiefnominatie__in.Contains(z.Archiefnominatie.Value))
            .WhereIf(filter.Archiefactiedatum != null, z => z.Archiefactiedatum == filter.Archiefactiedatum)
            .WhereIf(filter.Archiefactiedatum__gt != null, z => z.Archiefactiedatum > filter.Archiefactiedatum__gt)
            .WhereIf(filter.Archiefactiedatum__lt != null, z => z.Archiefactiedatum < filter.Archiefactiedatum__lt)
            .WhereIf(filter.Zaaktype != null, z => z.Zaaktype == filter.Zaaktype)
            .WhereIf(filter.Uuid__in.Any(), z => filter.Uuid__in.Contains(z.Id))
            .WhereIf(filter.Zaaktype__in.Any(), z => filter.Zaaktype__in.Contains(z.Zaaktype))
            .WhereIf(filter.Bronorganisatie__in.Any(), z => filter.Bronorganisatie__in.Contains(z.Bronorganisatie))
            .WhereIf(
                filter.Archiefactiedatum__isnull.HasValue,
                z =>
                    (filter.Archiefactiedatum__isnull.Value && z.Archiefactiedatum == null)
                    || (!filter.Archiefactiedatum__isnull.Value && z.Archiefactiedatum != null)
            )
            .WhereIf(filter.Registratiedatum != null, z => z.Registratiedatum == filter.Registratiedatum)
            .WhereIf(filter.Einddatum != null, z => z.Einddatum == filter.Einddatum)
            .WhereIf(filter.Einddatum__gt != null, z => z.Einddatum > filter.Einddatum__gt)
            .WhereIf(filter.Einddatum__lt != null, z => z.Einddatum < filter.Einddatum__lt)
            .WhereIf(
                filter.Einddatum__isnull.HasValue,
                z => (filter.Einddatum__isnull.Value && z.Einddatum == null) || (!filter.Einddatum__isnull.Value && z.Einddatum != null)
            )
            .WhereIf(filter.Registratiedatum__gt != null, z => z.Registratiedatum > filter.Registratiedatum__gt)
            .WhereIf(filter.Registratiedatum__lt != null, z => z.Registratiedatum < filter.Registratiedatum__lt)
            .WhereIf(filter.EinddatumGepland != null, z => z.EinddatumGepland == filter.EinddatumGepland)
            .WhereIf(filter.EinddatumGepland__gt != null, z => z.EinddatumGepland > filter.EinddatumGepland__gt)
            .WhereIf(filter.EinddatumGepland__lt != null, z => z.EinddatumGepland < filter.EinddatumGepland__lt)
            .WhereIf(filter.UiterlijkeEinddatumAfdoening != null, z => z.UiterlijkeEinddatumAfdoening == filter.UiterlijkeEinddatumAfdoening)
            .WhereIf(filter.UiterlijkeEinddatumAfdoening__gt != null, z => z.UiterlijkeEinddatumAfdoening > filter.UiterlijkeEinddatumAfdoening__gt)
            .WhereIf(filter.UiterlijkeEinddatumAfdoening__lt != null, z => z.UiterlijkeEinddatumAfdoening < filter.UiterlijkeEinddatumAfdoening__lt)
            .WhereIf(filter.Rol__betrokkeneType != null, z => z.ZaakRollen.Any(r => r.BetrokkeneType == filter.Rol__betrokkeneType))
            .WhereIf(filter.Rol__betrokkene != null, z => z.ZaakRollen.Any(r => r.Betrokkene == filter.Rol__betrokkene))
            .WhereIf(filter.Rol__omschrijvingGeneriek != null, z => z.ZaakRollen.Any(r => r.OmschrijvingGeneriek == filter.Rol__omschrijvingGeneriek))
            .WhereIf(
                filter.MaximaleVertrouwelijkheidaanduiding != null,
                z => z.VertrouwelijkheidAanduiding <= filter.MaximaleVertrouwelijkheidaanduiding
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.natuurlijk_persoon
                        && r.NatuurlijkPersoon.InpBsn == filter.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.natuurlijk_persoon
                        && r.NatuurlijkPersoon.AnpIdentificatie == filter.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.natuurlijk_persoon
                        && r.NatuurlijkPersoon.InpANummer == filter.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.niet_natuurlijk_persoon
                        && r.NietNatuurlijkPersoon.InnNnpId == filter.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.niet_natuurlijk_persoon
                        && r.NietNatuurlijkPersoon.AnnIdentificatie == filter.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.vestiging
                        && r.Vestiging.VestigingsNummer == filter.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__medewerker__identificatie != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.medewerker
                        && r.Medewerker.Identificatie == filter.Rol__betrokkeneIdentificatie__medewerker__identificatie
                    )
            )
            .WhereIf(
                filter.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie != null,
                z =>
                    z.ZaakRollen.Any(r =>
                        r.BetrokkeneType == BetrokkeneType.organisatorische_eenheid
                        && r.OrganisatorischeEenheid.Identificatie == filter.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie
                    )
            );
    }

    internal static IOrderedQueryable<Zaak> OrderBy(this IQueryable<Zaak> zaken, string order)
    {
        if (order != null && order.StartsWith('-'))
        {
            return zaken.OrderByDescending(GetZaakOrdering(order));
        }

        return zaken.OrderBy(GetZaakOrdering(order));
    }

    private static Expression<Func<Zaak, object>> GetZaakOrdering(string order)
    {
        return order?.TrimStart('-') switch
        {
            "startdatum" => z => z.Startdatum,
            "einddatum" => z => z.Einddatum,
            "publicatiedatum" => z => z.Publicatiedatum,
            "archiefactiedatum" => z => z.Archiefactiedatum,
            "registratiedatum" => z => z.Registratiedatum,
            "identificatie" => z => z.Identificatie,
            _ => z => z.CreationTime,
        };
    }
}

class GetAllZakenQuery : IRequest<QueryResult<PagedResult<Zaak>>>
{
    public GetAllZakenFilter GetAllZakenFilter { get; internal init; }
    public Geometry WithinZaakGeometry { get; internal init; }
    public PaginationFilter Pagination { get; internal init; }
    public string Ordering { get; internal init; }
    public int SRID { get; internal init; } = 28992;
}
