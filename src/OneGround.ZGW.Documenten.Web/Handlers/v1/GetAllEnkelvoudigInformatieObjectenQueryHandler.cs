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
using OneGround.ZGW.Documenten.Web.Models.v1;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class GetAllEnkelvoudigInformatieObjectenQueryHandler
    : DocumentenBaseHandler<GetAllEnkelvoudigInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllEnkelvoudigInformatieObjectenQuery, QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    private readonly DrcDbContext _context;
    private readonly IDistributedCacheHelper _cache;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    // TTL for cached count. Count for a given (rsin, page, filter) tuple are stable within this window.
    private static readonly TimeSpan CountCacheLifetime = TimeSpan.FromMinutes(5);

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

            var authPairs = await _context.TempInformatieObjectAuthorization.AsNoTracking().ToListAsync(cancellationToken);
            query = query.Where(BuildInlineAuthorizationPredicate(authPairs));
        }

        // Count with authorization reuses the same query (it already carries the inline VHA-grouped
        // auth predicate), so no temp-table JOIN is needed. Without a selectivity filter the planner
        // may pick a BitmapOr that turns lossy at scale and triggers millions of heap rechecks (~29s);
        // GetAuthorizationCountCachedAsync forces enable_bitmapscan=off so it uses the
        // (owner, iot, vha) covering index as a single Index-Only Scan + aggregate — O(N), no heap.
        var totalCount = hasAuthorizationFilter
            ? await GetAuthorizationCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken)
            : await GetTotalCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken);

        // Phase 1: Get page IDs using a narrow SELECT so the planner uses the (owner, id) index
        // with early termination instead of materializing all matching rows.
        var pageIds = await query
            .OrderByDescending(e => e.CreationTime)
            .ThenBy(e => e.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        // Phase 2: Fetch complete data for only the matched IDs (PK lookups).
        var pagedResult =
            pageIds.Count > 0
                ? await _context
                    .EnkelvoudigInformatieObjecten.AsNoTracking()
                    .Where(e => pageIds.Contains(e.Id))
                    .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
                    .OrderByDescending(e => e.CreationTime)
                    .ThenBy(e => e.Id)
                    .ToListAsync(cancellationToken)
                : [];

        var result = new PagedResult<EnkelvoudigInformatieObject> { PageResult = pagedResult, Count = totalCount };
        return new QueryResult<PagedResult<EnkelvoudigInformatieObject>>(result, QueryStatus.OK);
    }

    private async Task<int> GetAuthorizationCountCachedAsync(
        IQueryable<EnkelvoudigInformatieObject> query,
        GetAllEnkelvoudigInformatieObjectenFilter filterModel,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filterModel });

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
                bool anyFiltersSet = !string.IsNullOrEmpty(filterModel.Identificatie) || !string.IsNullOrEmpty(filterModel.Bronorganisatie);

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
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllEnkelvoudigInformatieObjectenFilter = filter });

        return await _cache.GetAsync(
            key,
            factory: async () => await query.CountAsync(cancellationToken),
            absoluteExpirationRelativeToNow: CountCacheLifetime,
            cancellationToken
        );
    }

    // Builds: (iot == 'x' && (int)vha.Value <= 5) || (iot == 'y' && (int)vha.Value <= 3) || ...
    // Inline constants remove the temp-table reference so PostgreSQL uses the (owner, id) index
    // with early termination via LIMIT — no Sort node needed.
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
        return e =>
            (filter.Bronorganisatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Bronorganisatie == filter.Bronorganisatie)
            && (filter.Identificatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Identificatie == filter.Identificatie);
    }
}

class GetAllEnkelvoudigInformatieObjectenQuery : IRequest<QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    public GetAllEnkelvoudigInformatieObjectenFilter GetAllEnkelvoudigInformatieObjectenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
