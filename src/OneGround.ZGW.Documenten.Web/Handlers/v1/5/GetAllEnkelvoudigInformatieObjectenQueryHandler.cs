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

            // Fetch auth pairs into C# to build an inline SQL predicate (OR chain of constants).
            // Any reference to the temp table in the paginated query causes PostgreSQL to rewrite the
            // filter as a semi-join where the 20-row temp table becomes the outer loop — forcing a Sort
            // of all ~1M matching rows before LIMIT takes effect (214s). Inlining the pairs as SQL
            // constants removes the join entirely so PostgreSQL can use the (owner, id) index scan with
            // early termination. NULL LatestVertrouwelijkheidAanduiding → UNKNOWN in SQL → row excluded.
            var authPairs = await _context.TempInformatieObjectAuthorization.AsNoTracking().ToListAsync(cancellationToken);

            query = query.Where(BuildInlineAuthorizationPredicate(authPairs));
        }

        // For count with authorization: use a JOIN-based query (not the inline OR predicate) so
        // PostgreSQL can pick a Hash Join. The inline OR predicate has no join → PostgreSQL picks
        // BitmapOr which becomes lossy at scale and causes massive heap rechecks (4M+ rows, 29s).
        // A JOIN with enable_nestloop=off forces a single streaming Hash Join pass — O(N+M).
        var totalCount = hasAuthorizationFilter
            ? await GetAuthorizationCountCachedAsync(rsinFilter, filter, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken)
            : await GetTotalCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken);

        // Phase 1: Get page IDs using a narrow SELECT so the planner uses the sorted (owner, id)
        // covering index with early termination, instead of materializing all matching rows.

        var pageIds =
            _configuration.GetValue<bool?>("Application:EnkelvoudigInformatieObjectenCursorPaging") ?? true
                ? await GetAnchorPagedResult(request, query, cancellationToken)
                : await GetOffsetPagedResult(request, query, cancellationToken);

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

    private static async Task<List<Guid>> GetOffsetPagedResult(
        GetAllEnkelvoudigInformatieObjectenQuery request,
        IQueryable<EnkelvoudigInformatieObject> query,
        CancellationToken cancellationToken
    )
    {
        return await query
            .OrderBy(e => e.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Guid>> GetAnchorPagedResult(
        GetAllEnkelvoudigInformatieObjectenQuery request,
        IQueryable<EnkelvoudigInformatieObject> query,
        CancellationToken cancellationToken
    )
    {
        var filterHash = ObjectHasher.ComputeSha1Hash(request.GetAllEnkelvoudigInformatieObjectenFilter);

        Guid? anchor = null;
        if (request.Pagination.Page > 1)
        {
            var previousAnchorKey = $"anchors:{_rsin}:{request.Pagination.Page - 2}:{filterHash}";

            // Look up the previous page's anchor in the distributed cache.
            // Note: A sentinel (Guid.Empty) is cached when no real anchor is available yet,
            // so the factory isn't re-invoked on subsequent lookups within the TTL. This means
            // that if a deep page is requested before page 1, that page (and pages between)
            // will fall back to the offset method until the sentinel expires - which is
            // functionally correct, just less optimal.
            var cachedAnchor = await _cache.GetAsync(
                previousAnchorKey,
                factory: () => Task.FromResult(Guid.Empty),
                absoluteExpirationRelativeToNow: AnchorCacheLifetime,
                cancellationToken
            );

            if (cachedAnchor != Guid.Empty)
            {
                anchor = cachedAnchor;
            }
        }

        bool useSeekMethod = anchor.HasValue;

        var orderedQuery = useSeekMethod
            ? query
                // Seek method: keyset pagination using the last seen Id as the anchor for the next page
                .Where(e => e.Id.CompareTo(anchor.Value) > 0)
                .OrderBy(e => e.Id)
                .Take(request.Pagination.Size)
            : query
                // Offset method: traditional pagination using Skip/Take (very inefficient for deep pages)
                .OrderBy(e => e.Id)
                .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
                .Take(request.Pagination.Size);

        var result = await orderedQuery.Select(e => e.Id).ToListAsync(cancellationToken);

        // Store anchor for the next page when we have a full page of results (or this is any page)
        if (result.Count > 0 && (!useSeekMethod || result.Count == request.Pagination.Size))
        {
            var lastId = result[^1];

            var currentAnchorKey = $"anchors:{_rsin}:{request.Pagination.Page - 1}:{filterHash}";

            // Note: GetAsync uses get-or-add semantics. If an entry already exists for this key
            // within the TTL, the existing entry is kept. The anchor for a given (rsin, page, filter)
            // tuple is stable within the TTL window, so this is acceptable.
            await _cache.GetAsync(
                currentAnchorKey,
                factory: () => Task.FromResult(lastId),
                absoluteExpirationRelativeToNow: AnchorCacheLifetime,
                cancellationToken
            );
        }

        return result;
    }

    private async Task<int> GetAuthorizationCountCachedAsync(
        Expression<Func<EnkelvoudigInformatieObject, bool>> rsinFilter,
        Expression<Func<EnkelvoudigInformatieObject, bool>> filter,
        GetAllEnkelvoudigInformatieObjectenFilter filterModel,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filterModel });

        return await _cache.GetAsync(
            key,
            factory: async () =>
            {
                // Without selectivity filters the result set is ~1M rows and the planner severely
                // underestimates cardinality, causing it to prefer Nested Loop. With filters the
                // result is small enough that the planner chooses correctly on its own.
                bool anyFiltersSet =
                    !string.IsNullOrEmpty(filterModel.Identificatie)
                    || !string.IsNullOrEmpty(filterModel.Bronorganisatie)
                    || (filterModel.Uuid_In != null && filterModel.Uuid_In.Any())
                    || (filterModel.Trefwoorden_In != null && filterModel.Trefwoorden_In.Any());

                // SET LOCAL requires an active transaction. Settings revert automatically when
                // the transaction is disposed — safe for pooled connections.
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                if (!anyFiltersSet)
                {
                    await _context.Database.ExecuteSqlRawAsync("SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB';", cancellationToken);
                }

                return await _context
                    .EnkelvoudigInformatieObjecten.AsNoTracking()
                    .Where(rsinFilter)
                    .Where(filter)
                    .Join(
                        _context.TempInformatieObjectAuthorization,
                        o => o.InformatieObjectType,
                        a => a.InformatieObjectType,
                        (o, a) => new { Object = o, Auth = a }
                    )
                    .Where(oa => (int)oa.Object.LatestVertrouwelijkheidAanduiding.Value <= oa.Auth.MaximumVertrouwelijkheidAanduiding)
                    .CountAsync(cancellationToken);
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
        // Create a key for the current request+ClientId (uri contains the query-parameters as well)
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filter });

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

    // Builds: (iot == 'x' && (int)vha.Value <= 5) || (iot == 'y' && (int)vha.Value <= 3) || ...
    // Using inline constants (no temp-table reference) forces PostgreSQL to use the (owner, id) index
    // scan with per-row filter evaluation and early termination via LIMIT — no Sort node needed.
    private static Expression<Func<EnkelvoudigInformatieObject, bool>> BuildInlineAuthorizationPredicate(
        List<TempInformatieObjectAuthorization> authPairs
    )
    {
        var param = Expression.Parameter(typeof(EnkelvoudigInformatieObject), "o");

        if (authPairs.Count == 0)
            return Expression.Lambda<Func<EnkelvoudigInformatieObject, bool>>(Expression.Constant(false), param);

        Expression? body = null;
        foreach (var pair in authPairs)
        {
            var iotProp = Expression.Property(param, nameof(EnkelvoudigInformatieObject.InformatieObjectType));
            var iotEq = Expression.Equal(iotProp, Expression.Constant(pair.InformatieObjectType));

            var vhaNullable = Expression.Property(param, nameof(EnkelvoudigInformatieObject.LatestVertrouwelijkheidAanduiding));
            var vhaInt = Expression.Convert(Expression.Property(vhaNullable, "Value"), typeof(int));
            var vhaLe = Expression.LessThanOrEqual(vhaInt, Expression.Constant(pair.MaximumVertrouwelijkheidAanduiding));

            var pairExpr = Expression.AndAlso(iotEq, vhaLe);
            body = body == null ? pairExpr : Expression.OrElse(body, pairExpr);
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
