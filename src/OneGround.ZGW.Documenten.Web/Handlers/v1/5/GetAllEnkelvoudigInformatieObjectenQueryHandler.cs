using System;
using System.Collections.Generic;
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
using OneGround.ZGW.Documenten.DataModel.Authorization;
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
    private readonly IConfiguration _configuration;

    // TTL for cached count/page anchors. Count/Anchors for a given (rsin, page, filter) tuple are stable within this window.
    private static readonly TimeSpan CountCacheLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AnchorCacheLifetime = TimeSpan.FromMinutes(5);

    // Returned as the total count when the count query exceeds the command timeout on very large tenants.
    // The unfiltered COUNT over the full owner partition can run for several seconds; rather than failing the
    // whole request, we return this sentinel so the caller still gets a page. The real count reappears once the
    // query completes within the timeout (e.g. after caches warm up); the sentinel is deliberately not cached.
    private const int CountTimeoutSentinel = 999_999_999;

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
        _configuration = configuration;
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

            // Fetch auth pairs into C# and build an inline predicate grouped by VHA level.
            // Any temp-table reference in the paginated query causes PostgreSQL to rewrite it as a
            // semi-join where the temp table becomes the outer loop — forcing a Sort of all ~1M matching
            // rows before LIMIT takes effect (214s). Inlining the data removes the join so PostgreSQL
            // can use the sorted (owner, creationtime, id) index with early termination via LIMIT.
            // Pairs are grouped by MaxVha: each group becomes iot = ANY(@types) AND vha <= maxVha.
            // This collapses N pairs (up to 5000) into ≤7 OR terms (one per ZGW VHA level), keeping
            // the predicate cheap to plan and execute regardless of how many types are authorized.
            var authPairs = await _context.TempInformatieObjectAuthorization.AsNoTracking().ToListAsync(cancellationToken);

            query = query.Where(BuildInlineAuthorizationPredicate(authPairs));
        }

        // Count with authorization reuses the same query (it already carries the inline VHA-grouped
        // auth predicate), so no temp-table JOIN is needed. Without a selectivity filter the planner
        // may pick a BitmapOr that turns lossy at scale and triggers millions of heap rechecks (~29s);
        // GetAuthorizationCountCachedAsync forces enable_bitmapscan=off so it uses the
        // (owner, iot, vha) covering index as a single Index-Only Scan + aggregate — O(N), no heap.
        int totalCount;
        try
        {
            totalCount = hasAuthorizationFilter
                ? await GetAuthorizationCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken)
                : await GetTotalCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken);
        }
        catch (Exception ex) when (IsCountTimeout(ex, cancellationToken))
        {
            // Don't fail the whole listing because the count is too expensive on this tenant — return a
            // sentinel count and still serve the page. The factory threw before caching, so nothing poisoned
            // the cache and the next request retries the real count.
            _logger.LogWarning(ex, "Count query timed out for rsin {Rsin}; returning sentinel count {Sentinel}.", _rsin, CountTimeoutSentinel);
            totalCount = CountTimeoutSentinel;
        }

        // Phase 1: Get page IDs using a narrow SELECT so the planner uses the sorted
        // (owner, creationtime, id) covering index (t3b_IX_eio_owner_creationtime_id_incl_type_vha)
        // with early termination, instead of materializing all matching rows.

        var pageIds =
            _configuration.GetValue<bool?>("Application:EnkelvoudigInformatieObjectenCursorPaging") ?? true
                ? await GetAnchorPagedResult(request, query, cancellationToken)
                : await GetOffsetPagedResult(request, query, cancellationToken);

        // Phase 2: Fetch complete data for only the matched IDs (typically 100 PK lookups).
        var pagedResult =
            pageIds.Count > 0
                ? await OrderByPage(
                        _context
                            .EnkelvoudigInformatieObjecten.AsNoTracking()
                            .Where(e => pageIds.Contains(e.Id))
                            .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                                .ThenInclude(e => e.BestandsDelen)
                    )
                    .ToListAsync(cancellationToken)
                : [];

        var result = new PagedResult<EnkelvoudigInformatieObject> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<EnkelvoudigInformatieObject>>(result, QueryStatus.OK);
    }

    private static async Task<List<Guid>> GetOffsetPagedResult(
        GetAllEnkelvoudigInformatieObjectenQuery request,
        IQueryable<EnkelvoudigInformatieObject> query,
        CancellationToken cancellationToken
    )
    {
        return await OrderByPage(query)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    // Single source of truth for the page ordering. The keyset seek predicate in
    // GetAnchorPagedResult is only correct while every ORDER BY matches this exact tuple,
    // so all paging paths (offset, seek, and the Phase-2 fetch) must order through here.
    private static IOrderedQueryable<EnkelvoudigInformatieObject> OrderByPage(IQueryable<EnkelvoudigInformatieObject> query) =>
        query.OrderByDescending(e => e.CreationTime).ThenBy(e => e.Id);

    private async Task<List<Guid>> GetAnchorPagedResult(
        GetAllEnkelvoudigInformatieObjectenQuery request,
        IQueryable<EnkelvoudigInformatieObject> query,
        CancellationToken cancellationToken
    )
    {
        var filterHash = ObjectHasher.ComputeSha1Hash(request.GetAllEnkelvoudigInformatieObjectenFilter);

        PageAnchor? anchor = null;
        if (request.Pagination.Page > 1)
        {
            var previousAnchorKey = $"anchors:{_rsin}:{request.Pagination.Page - 2}:{filterHash}";

            // Look up the previous page's composite anchor in the distributed cache.
            // A sentinel (PageAnchor.Sentinel) is stored when no real anchor is available yet,
            // so the factory isn't re-invoked on subsequent lookups within the TTL. If a deep
            // page is requested before page 1, it falls back to the offset method until the
            // sentinel expires — functionally correct, just less optimal.
            var cachedAnchor = await _cache.GetAsync(
                previousAnchorKey,
                factory: () => Task.FromResult(PageAnchor.Sentinel),
                absoluteExpirationRelativeToNow: AnchorCacheLifetime,
                cancellationToken
            );

            if (cachedAnchor != PageAnchor.Sentinel)
                anchor = cachedAnchor;
        }

        bool useSeekMethod = anchor != null;

        IQueryable<EnkelvoudigInformatieObject> orderedQuery;
        if (useSeekMethod)
        {
            // Seek method: keyset pagination using (CreationTime, Id) as the composite anchor.
            // Rows "after" the anchor in (CreationTime DESC, Id ASC) order are exactly those matching
            // the OR-form:  CreationTime < anchor.CreationTime
            //               OR (CreationTime == anchor.CreationTime AND Id > anchor.Id)
            //
            // That OR-form alone can never be a btree index-range bound (the mixed sort directions rule
            // out a row-comparison rewrite), so PostgreSQL keeps it as a Filter and starts the scan at
            // the top of the index — deep pages then re-scan everything newer than the anchor (offset-
            // like cost). The leading "CreationTime <= anchor.CreationTime" conjunct fixes that: it is
            // logically implied by both OR branches (so results are unchanged) but IS sargable, so
            // PostgreSQL uses it as an Index Cond to start the scan at the anchor. The OR-form then only
            // disambiguates the boundary rows (same CreationTime, Id tiebreak).
            orderedQuery = OrderByPage(
                    query.Where(e =>
                        e.CreationTime <= anchor!.CreationTime
                        && (e.CreationTime < anchor.CreationTime || (e.CreationTime == anchor.CreationTime && e.Id > anchor.Id))
                    )
                )
                .Take(request.Pagination.Size);
        }
        else
        {
            // Offset method: traditional Skip/Take fallback when no anchor is cached.
            orderedQuery = OrderByPage(query).Skip(request.Pagination.Size * (request.Pagination.Page - 1)).Take(request.Pagination.Size);
        }

        // Phase 1 selects both Id and CreationTime — both are needed to build the composite anchor.
        var pageData = await orderedQuery.Select(e => new { e.Id, e.CreationTime }).ToListAsync(cancellationToken);

        var result = pageData.ConvertAll(x => x.Id);

        if (result.Count > 0 && (!useSeekMethod || result.Count == request.Pagination.Size))
        {
            var last = pageData[^1];
            var currentAnchorKey = $"anchors:{_rsin}:{request.Pagination.Page - 1}:{filterHash}";

            // GetAsync uses get-or-add semantics — if a key already exists within the TTL the
            // existing entry is kept. The anchor for a given (rsin, page, filter) is stable
            // within the TTL window, so this is acceptable.
            await _cache.GetAsync(
                currentAnchorKey,
                factory: () => Task.FromResult(new PageAnchor(last.CreationTime, last.Id)),
                absoluteExpirationRelativeToNow: AnchorCacheLifetime,
                cancellationToken
            );
        }

        return result;
    }

    // Composite keyset anchor for cursor-based pagination on (CreationTime DESC, Id ASC).
    // Sentinel is stored in cache to prevent repeated factory calls when no real anchor exists yet.
    private sealed record PageAnchor(DateTime CreationTime, Guid Id)
    {
        public static readonly PageAnchor Sentinel = new(DateTime.MinValue, Guid.Empty);
    }

    private async Task<int> GetAuthorizationCountCachedAsync(
        IQueryable<EnkelvoudigInformatieObject> query,
        GetAllEnkelvoudigInformatieObjectenFilter filterModel,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { Rsin = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filterModel });

        return await _cache.GetAsync(
            key,
            factory: async () =>
            {
                // The query already carries the inline VHA-grouped auth predicate, so this is just a
                // COUNT over it — no temp-table JOIN. Unlike the paged query there is no LIMIT to
                // terminate early, so without a selectivity filter (~1M rows) the planner may pick a
                // BitmapOr that turns lossy and triggers millions of heap rechecks (~29s). Forcing
                // enable_bitmapscan=off makes it use the (owner, iot, vha) covering index as a single
                // Index-Only Scan + filter + aggregate. With a filter the result is small enough that
                // the planner chooses correctly on its own.
                bool anyFiltersSet =
                    !string.IsNullOrEmpty(filterModel.Identificatie)
                    || !string.IsNullOrEmpty(filterModel.Bronorganisatie)
                    || (filterModel.Uuid_In != null && filterModel.Uuid_In.Any())
                    || (filterModel.Trefwoorden_In != null && filterModel.Trefwoorden_In.Any());

                if (!anyFiltersSet)
                {
                    // SET LOCAL requires an active transaction. Settings revert automatically when
                    // the transaction is disposed — safe for pooled connections.
                    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                    await _context.Database.ExecuteSqlRawAsync("SET LOCAL enable_bitmapscan = off;", cancellationToken);

                    return await query.CountAsync(cancellationToken);
                }

                return await query.CountAsync(cancellationToken);
            },
            absoluteExpirationRelativeToNow: CountCacheLifetime,
            cancellationToken
        );
    }

    private async Task<int> GetTotalCountCachedAsync(
        IQueryable<EnkelvoudigInformatieObject> query,
        GetAllEnkelvoudigInformatieObjectenFilter filter,
        CancellationToken cancellationToken
    )
    {
        // Create a key for the current request+Rsin (uri contains the query-parameters as well)
        var key = ObjectHasher.ComputeSha1Hash(new { Rsin = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filter });

        // Note: Cache the Count from SQL for 5 minutes to avoid repeated expensive queries until the cache expires.
        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                return await query.CountAsync(cancellationToken);
            },
            absoluteExpirationRelativeToNow: CountCacheLifetime,
            cancellationToken
        );

        return totalCount;
    }

    // True only for a database command timeout on the count query — never for a genuine caller/client
    // cancellation, which must keep bubbling up. A client-side Npgsql command timeout surfaces as an
    // exception chain containing a TimeoutException; a server-side statement_timeout surfaces as a
    // PostgresException with SqlState 57014 (query_canceled).
    private static bool IsCountTimeout(Exception ex, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is TimeoutException)
                return true;
            if (current is PostgresException { SqlState: PostgresErrorCodes.QueryCanceled })
                return true;
        }

        return false;
    }

    // Builds: (iot = ANY(@types_for_vha5) AND vha <= 5) OR (iot = ANY(@types_for_vha3) AND vha <= 3) OR ...
    // Groups N auth pairs by MaxVha so the predicate has ≤7 OR terms (one per ZGW VHA level) regardless
    // of how many types are authorized. List.Contains() translates to = ANY(ARRAY[...]) in PostgreSQL —
    // a single operator, efficient for large type sets. NULL LatestVertrouwelijkheidAanduiding evaluates
    // to UNKNOWN in SQL → row excluded, matching the old JOIN behavior.
    private static Expression<Func<EnkelvoudigInformatieObject, bool>> BuildInlineAuthorizationPredicate(
        List<TempInformatieObjectAuthorization> authPairs
    )
    {
        var param = Expression.Parameter(typeof(EnkelvoudigInformatieObject), "o");

        if (authPairs.Count == 0)
            return Expression.Lambda<Func<EnkelvoudigInformatieObject, bool>>(Expression.Constant(false), param);

        // Order groups by VHA and types within each group so the generated SQL is deterministic
        // regardless of the (unordered) temp-table fetch order — stable SQL text => plan-cache reuse.
        var grouped = authPairs
            .GroupBy(p => p.MaximumVertrouwelijkheidAanduiding)
            .OrderBy(g => g.Key)
            .Select(g => (MaxVha: g.Key, Types: g.Select(p => p.InformatieObjectType).OrderBy(t => t).ToList()))
            .ToList();

        var iotProp = Expression.Property(param, nameof(EnkelvoudigInformatieObject.InformatieObjectType));
        var vhaNullable = Expression.Property(param, nameof(EnkelvoudigInformatieObject.LatestVertrouwelijkheidAanduiding));
        var vhaInt = Expression.Convert(Expression.Property(vhaNullable, "Value"), typeof(int));
        var containsMethod = typeof(List<string>).GetMethod(nameof(List<string>.Contains))!;

        Expression? body = null;
        foreach (var (maxVha, types) in grouped)
        {
            var containsExpr = Expression.Call(Expression.Constant(types), containsMethod, iotProp);
            var vhaLe = Expression.LessThanOrEqual(vhaInt, Expression.Constant(maxVha));
            var groupExpr = Expression.AndAlso(containsExpr, vhaLe);
            body = body == null ? groupExpr : Expression.OrElse(body, groupExpr);
        }

        return Expression.Lambda<Func<EnkelvoudigInformatieObject, bool>>(body!, param);
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
        // sorted (owner, creationtime, id) covering index with early termination.
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
