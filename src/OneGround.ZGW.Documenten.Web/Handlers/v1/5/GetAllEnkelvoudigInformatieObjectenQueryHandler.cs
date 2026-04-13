using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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

        // For count, use a JOIN-based query instead of EXISTS when authorization filtering is active.
        // The EXISTS subquery is optimal for paginated queries (early termination per-row) but causes
        // 1M individual index lookups for count. A JOIN lets PostgreSQL use a Hash Join — single
        // streaming pass over the versie table — which is O(N+M) instead of O(N × log M).
        var totalCount = hasAuthorizationFilter
            ? await GetAuthorizationCountCachedAsync(rsinFilter, filter, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken)
            : await GetTotalCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken);

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

    private async Task<int> GetTotalCountCachedAsync(
        IQueryable<EnkelvoudigInformatieObject> query,
        GetAllEnkelvoudigInformatieObjectenFilter filter,
        CancellationToken cancellationToken
    )
    {
        // Create a key for the current request+ClientId (uri contains the query-parameters as well)
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filter });

        // Note: Cache the Count from SQL for 5 minutes to avoid repeated expensive queries until the cache expires.
        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                return await query.CountAsync(cancellationToken);
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5),
            cancellationToken
        );

        return totalCount;
    }

    private async Task<int> GetAuthorizationCountCachedAsync(
        Expression<Func<EnkelvoudigInformatieObject, bool>> rsinFilter,
        Expression<Func<EnkelvoudigInformatieObject, bool>> filter,
        GetAllEnkelvoudigInformatieObjectenFilter filterModel,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filterModel });

        bool anyFiltersSet =
            !string.IsNullOrEmpty(filterModel.Identificatie)
            || !string.IsNullOrEmpty(filterModel.Bronorganisatie)
            || (filterModel.Uuid_In != null && filterModel.Uuid_In.Any())
            || (filterModel.Trefwoorden_In != null && filterModel.Trefwoorden_In.Any());

        // Note: Cache the Count from SQL for 5 minutes to avoid repeated expensive queries until the cache expires.
        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                if (!anyFiltersSet)
                {
                    // The planner severely underestimates cardinality (7K vs 1M actual) due to
                    // stale/missing statistics on the temporary authorization table. This makes it
                    // prefer Nested Loop (1M individual B-tree lookups = ~100s) over Hash Join
                    // (single streaming pass = ~5s). Temporarily disable nested loops and increase
                    // work_mem so the planner picks Hash Join for both the EIO→versie and auth joins.
                    // SET LOCAL requires an active transaction — without one, PostgreSQL silently
                    // treats it as session-scoped SET. Settings revert automatically when the
                    // transaction is disposed (no manual RESET needed, safe for pooled connections).
                    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                    await _context.Database.ExecuteSqlRawAsync("SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB';", cancellationToken);
                }

                var result = await _context
                    .EnkelvoudigInformatieObjecten.AsNoTracking()
                    .Where(rsinFilter)
                    .Where(filter)
                    .Join(
                        // Filter versie by owner too — without this, the Hash Join probes ALL
                        // 69M versie rows. With the filter, it only scans ~1M versie rows for
                        // this RSIN, using the existing (Owner, Id, Vertrouwelijkheidaanduiding) index.
                        _context.EnkelvoudigInformatieObjectVersies.Where(v => v.Owner == _rsin),
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
                    .CountAsync(cancellationToken);

                // Note: Settings automatically reverted — no RESET needed
                return result;
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5), // Note: Keep the calculated total count the same for 5 minutes!
            cancellationToken
        );

        return totalCount;
    }

    private static Expression<Func<EnkelvoudigInformatieObject, bool>> GetEnkelvoudigInformatieObjectFilterPredicate(
        GetAllEnkelvoudigInformatieObjectenFilter filter
    )
    {
        var filterUuid_In = filter
            .Uuid_In?.Select(uuid => Guid.TryParse(uuid, out var parsedUuid) ? parsedUuid : (Guid?)null)
            .Where(parsedUuid => parsedUuid != null)
            .Select(parsedUuid => parsedUuid!.Value)
            .ToList();

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
