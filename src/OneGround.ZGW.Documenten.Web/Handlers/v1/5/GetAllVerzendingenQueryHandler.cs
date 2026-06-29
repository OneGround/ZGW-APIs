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

class GetAllVerzendingenQueryHandler
    : DocumentenBaseHandler<GetAllVerzendingenQueryHandler>,
        IRequestHandler<GetAllVerzendingenQuery, QueryResult<PagedResult<Verzending>>>
{
    private readonly DrcDbContext _context;
    private readonly IDistributedCacheHelper _cache;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    public GetAllVerzendingenQueryHandler(
        ILogger<GetAllVerzendingenQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<Verzending>>> Handle(GetAllVerzendingenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Verzendingen....");

        var filter = GetVerzendingFilterPredicate(request.GetAllVerzendingenFilter);
        var rsinFilter = GetRsinFilterPredicate<Verzending>(o => o.InformatieObject.Owner == _rsin);

        var query = _context.Verzendingen.AsNoTracking().Where(rsinFilter).Where(filter);

        bool hasAuthorizationFilter = !_authorizationContext.Authorization.HasAllAuthorizations;
        if (hasAuthorizationFilter)
        {
            await _informatieObjectAuthorizationTempTableService.InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            // Fetch auth pairs into C# and build an inline predicate grouped by VHA level — same
            // approach as the EIO handler. Removing the temp-table reference from the paged query
            // lets PostgreSQL filter on inline constants instead of a per-row semi-join against the
            // temp table. The Verzending → EIO navigation (for InformatieObjectType and
            // LatestVertrouwelijkheidAanduiding, which live on EIO) remains.
            // NULL LatestVertrouwelijkheidAanduiding → UNKNOWN in SQL → row excluded, matching old behavior.
            var authPairs = await _context.TempInformatieObjectAuthorization.AsNoTracking().ToListAsync(cancellationToken);

            query = query.Where(BuildInlineAuthorizationPredicate(authPairs));
        }

        // Count with authorization reuses the same query (it already carries the inline VHA-grouped
        // auth predicate), so count and page now share the exact same predicate — no separate JOIN
        // formulation that could drift. When no selectivity filter is set GetAuthorizationCountCachedAsync
        // forces enable_bitmapscan = off (same as the EIO count) to avoid a lossy BitmapOr on the OR-predicate.
        var totalCount = hasAuthorizationFilter
            ? await GetAuthorizationCountCachedAsync(query, request.GetAllVerzendingenFilter, cancellationToken)
            : await GetTotalCountCachedAsync(query, request.GetAllVerzendingenFilter, cancellationToken);

        // Phase 1: Get page IDs using a narrow SELECT so the planner can use early termination.
        var pageIds = await query
            .OrderBy(v => v.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        // Phase 2: Fetch complete data for only the matched IDs.
        var pagedResult =
            pageIds.Count > 0
                ? await _context
                    .Verzendingen.AsNoTracking()
                    .Where(v => pageIds.Contains(v.Id))
                    .Include(v => v.InformatieObject)
                    .OrderBy(v => v.Id)
                    .ToListAsync(cancellationToken)
                : [];

        var result = new PagedResult<Verzending> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Verzending>>(result, QueryStatus.OK);
    }

    private async Task<int> GetTotalCountCachedAsync(
        IQueryable<Verzending> query,
        GetAllVerzendingenFilter filter,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllVerzendingenFilter = filter });

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
        IQueryable<Verzending> query,
        GetAllVerzendingenFilter filterModel,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllVerzendingenFilter = filterModel });

        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                // The query already carries the inline VHA-grouped auth predicate, so this is just a
                // COUNT over it (Verzending joined to EIO via the InformatieObject navigation). Unlike
                // the paged query there is no LIMIT to terminate early, so without a selectivity filter
                // the planner may pick a BitmapOr on the OR-predicate that turns lossy at scale and
                // triggers heap rechecks. Forcing enable_bitmapscan = off (same lever as the EIO count)
                // steers it to a plain index scan + filter; the Verzending → EIO join strategy is left to
                // the planner, which now has real stats (no temp table in the query). With a filter the
                // result set is small enough that the planner chooses correctly on its own.
                bool anyFiltersSet = filterModel.InformatieObject != null || filterModel.Betrokkene != null || filterModel.AardRelatie.HasValue;

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
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5),
            cancellationToken
        );

        return totalCount;
    }

    // Builds: (iot = ANY(@types_for_vha5) AND vha <= 5) OR (iot = ANY(@types_for_vha3) AND vha <= 3) OR ...
    // over the Verzending → InformatieObject (EIO) navigation. Mirrors the EIO handler's predicate:
    // groups N auth pairs by MaxVha into ≤7 OR terms, List.Contains() → = ANY(ARRAY[...]) in PostgreSQL.
    // NULL LatestVertrouwelijkheidAanduiding evaluates to UNKNOWN in SQL → row excluded.
    private static Expression<Func<Verzending, bool>> BuildInlineAuthorizationPredicate(List<TempInformatieObjectAuthorization> authPairs)
    {
        var param = Expression.Parameter(typeof(Verzending), "v");

        if (authPairs.Count == 0)
            return Expression.Lambda<Func<Verzending, bool>>(Expression.Constant(false), param);

        var grouped = authPairs
            .GroupBy(p => p.MaximumVertrouwelijkheidAanduiding)
            .Select(g => (MaxVha: g.Key, Types: g.Select(p => p.InformatieObjectType).ToList()))
            .ToList();

        // v.InformatieObject.{InformatieObjectType, LatestVertrouwelijkheidAanduiding}
        var infoObj = Expression.Property(param, nameof(Verzending.InformatieObject));
        var iotProp = Expression.Property(infoObj, nameof(EnkelvoudigInformatieObject.InformatieObjectType));
        var vhaNullable = Expression.Property(infoObj, nameof(EnkelvoudigInformatieObject.LatestVertrouwelijkheidAanduiding));
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

        return Expression.Lambda<Func<Verzending, bool>>(body!, param);
    }

    private Expression<Func<Verzending, bool>> GetVerzendingFilterPredicate(GetAllVerzendingenFilter filter)
    {
        return v =>
            (filter.InformatieObject == null || v.InformatieObject.Id == _uriService.GetId(filter.InformatieObject))
            && (filter.Betrokkene == null || v.Betrokkene == filter.Betrokkene)
            && (!filter.AardRelatie.HasValue || v.AardRelatie == filter.AardRelatie.Value);
    }
}

class GetAllVerzendingenQuery : IRequest<QueryResult<PagedResult<Verzending>>>
{
    public GetAllVerzendingenFilter GetAllVerzendingenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
