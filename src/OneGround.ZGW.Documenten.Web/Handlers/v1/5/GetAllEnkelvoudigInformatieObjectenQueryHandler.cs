using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1._5;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

class GetAllEnkelvoudigInformatieObjectenQueryHandler
    : DocumentenBaseHandler<GetAllEnkelvoudigInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllEnkelvoudigInformatieObjectenQuery, QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    private readonly DrcDbContext _context;
    private readonly IDistributedCacheHelper _cache;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    public GetAllEnkelvoudigInformatieObjectenQueryHandler(
        ILogger<GetAllEnkelvoudigInformatieObjectenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDistributedCacheHelper cache,
        IInformatieObjectAuthorizationTempTableService informatieObjectAuthorizationTempTableService,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _cache = cache;
        _informatieObjectAuthorizationTempTableService = informatieObjectAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<EnkelvoudigInformatieObject>>> Handle(
        GetAllEnkelvoudigInformatieObjectenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all EnkelvoudigInformatieObjecten....");

        var filter = GetEnkelvoudigInformatieObjectFilterPredicate(request.GetAllEnkelvoudigInformatieObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObject>();

        var query = _context.EnkelvoudigInformatieObjecten.AsNoTracking().Where(rsinFilter).Where(filter);

        bool hasAuthorizationFilter = !_authorizationContext.Authorization.HasAllAuthorizations;
        if (hasAuthorizationFilter)
        {
            await _informatieObjectAuthorizationTempTableService.InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            // Use explicit subquery instead of navigation property to avoid a top-level LEFT JOIN
            // to the versie table. With EXISTS, PostgreSQL can scan the (owner, id) covering index
            // in order and stop after LIMIT matches (early termination), instead of materializing all rows.
            query = query.Where(e =>
                _context.EnkelvoudigInformatieObjectVersies.Any(v =>
                    v.Id == e.LatestEnkelvoudigInformatieObjectVersieId
                    && _context.TempInformatieObjectAuthorization.Any(a =>
                        a.InformatieObjectType == e.InformatieObjectType && (int)v.Vertrouwelijkheidaanduiding <= a.MaximumVertrouwelijkheidAanduiding
                    )
                )
            );
        }

        // For count, use a JOIN-based query instead of EXISTS. The EXISTS subquery is optimal for
        // paginated queries (early termination per-row) but causes 1M individual index lookups for
        // count. A JOIN lets PostgreSQL use a Hash Join — single streaming pass over the versie
        // table — which is O(N+M) instead of O(N × log M).
        var totalCount = await GetTotalCountCachedAsync(
            hasAuthorizationFilter
                ? _context.EnkelvoudigInformatieObjecten.AsNoTracking()
                    .Where(rsinFilter)
                    .Where(filter)
                    .Join(
                        _context.EnkelvoudigInformatieObjectVersies,
                        e => e.LatestEnkelvoudigInformatieObjectVersieId,
                        v => v.Id,
                        (e, v) => new { e.InformatieObjectType, v.Vertrouwelijkheidaanduiding }
                    )
                    .Where(ev =>
                        _context.TempInformatieObjectAuthorization.Any(a =>
                            a.InformatieObjectType == ev.InformatieObjectType
                            && (int)ev.Vertrouwelijkheidaanduiding <= a.MaximumVertrouwelijkheidAanduiding
                        )
                    )
                : query.Select(e => new { e.InformatieObjectType, Vertrouwelijkheidaanduiding = 0 }),
            request.GetAllEnkelvoudigInformatieObjectenFilter,
            cancellationToken
        );

        // Phase 1: Get page IDs using a narrow SELECT so the planner uses the sorted (owner, id)
        // covering index with early termination, instead of materializing all matching rows.
        var pageIds = await query
            .OrderBy(e => e.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        // Phase 2: Fetch complete data for only the matched IDs (typically 100 PK lookups).
        var pagedResult =
            pageIds.Count > 0
                ? await _context
                    .EnkelvoudigInformatieObjecten.AsNoTracking()
                    .Where(e => pageIds.Contains(e.Id))
                    .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                        .ThenInclude(e => e.BestandsDelen)
                    .OrderBy(e => e.Id)
                    .ToListAsync(cancellationToken)
                : [];

        var result = new PagedResult<EnkelvoudigInformatieObject> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<EnkelvoudigInformatieObject>>(result, QueryStatus.OK);
    }

    private async Task<int> GetTotalCountCachedAsync<T>(
        IQueryable<T> query,
        GetAllEnkelvoudigInformatieObjectenFilter filter,
        CancellationToken cancellationToken
    )
    {
        // Create a key for the current request+ClientId (uri contains the query-parameters as well)
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filter });

        // Note: Cache the Count from SQL for 5 minutes. And also Note: If an error occurs (e.g. due to a complex query plan), log the error and cache -1 to avoid repeated expensive queries until the cache expires.
        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                try
                {
                    var result = await query.CountAsync(cancellationToken);
                    return result;
                }
                catch (NpgsqlException ex)
                {
                    _logger.LogError(ex, "Npgsql error, set total number of EnkelvoudigInformatieObjecten to -1");
                    return -1;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occured, set total number of EnkelvoudigInformatieObjecten to -1");
                    return -1;
                }
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5),
            cancellationToken
        );

        return totalCount;
    }

    private static Expression<Func<EnkelvoudigInformatieObject, bool>> GetEnkelvoudigInformatieObjectFilterPredicate(
        GetAllEnkelvoudigInformatieObjectenFilter filter
    )
    {
        var filterUuid_In = filter.Uuid_In?.Select(Guid.Parse).ToList();
        var filterTrefwoorden_In = filter.Trefwoorden_In?.ToList();

        // Only reference the versie navigation when versie-based filters are actually set.
        // This avoids an unnecessary LEFT JOIN that prevents the planner from using the
        // sorted (owner, id) covering index with early termination.
        bool hasVersieFilters = filter.Bronorganisatie != null || filter.Identificatie != null || filterTrefwoorden_In != null;

        if (!hasVersieFilters)
        {
            return e => filterUuid_In == null || filterUuid_In.Contains(e.Id);
        }

        return e =>
            (filter.Bronorganisatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Bronorganisatie == filter.Bronorganisatie)
            && (filter.Identificatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Identificatie == filter.Identificatie)
            && (filterUuid_In == null || filterUuid_In.Contains(e.Id))
            && (filterTrefwoorden_In == null || e.LatestEnkelvoudigInformatieObjectVersie.Trefwoorden.Any(t => filterTrefwoorden_In.Contains(t)));
    }
}

class GetAllEnkelvoudigInformatieObjectenQuery : IRequest<QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    public GetAllEnkelvoudigInformatieObjectenFilter GetAllEnkelvoudigInformatieObjectenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal init; }
}
