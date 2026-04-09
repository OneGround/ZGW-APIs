using System;
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

            // Use explicit subquery instead of navigation property to avoid a top-level LEFT JOIN
            // to the versie table. With EXISTS, PostgreSQL can use early termination for pagination.
            query = query.Where(v =>
                _context.EnkelvoudigInformatieObjectVersies.Any(ver =>
                    ver.Id == v.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId
                    && _context.TempInformatieObjectAuthorization.Any(a =>
                        a.InformatieObjectType == v.InformatieObject.InformatieObjectType
                        && (int)ver.Vertrouwelijkheidaanduiding <= a.MaximumVertrouwelijkheidAanduiding
                    )
                )
            );
        }

        // For count, use a JOIN-based query instead of EXISTS when authorization filtering is active.
        // The EXISTS subquery is optimal for paginated queries (early termination per-row) but causes
        // individual index lookups for count. A JOIN lets PostgreSQL use a Hash Join — single
        // streaming pass over the versie table — which is O(N+M) instead of O(N × log M).
        var totalCount = hasAuthorizationFilter
            ? await GetAuthorizationCountCachedAsync(rsinFilter, filter, request.GetAllVerzendingenFilter, cancellationToken)
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
        Expression<Func<Verzending, bool>> rsinFilter,
        Expression<Func<Verzending, bool>> filter,
        GetAllVerzendingenFilter filterModel,
        CancellationToken cancellationToken
    )
    {
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllVerzendingenFilter = filterModel });

        int totalCount = await _cache.GetAsync(
            key,
            factory: async () =>
            {
                // The planner severely underestimates cardinality due to stale/missing statistics
                // on the temporary authorization table. Temporarily disable nested loops and increase
                // work_mem so the planner picks Hash Join for the versie join.
                // SET LOCAL requires an active transaction — without one, PostgreSQL silently
                // treats it as session-scoped SET. Settings revert automatically when the
                // transaction is disposed (no manual RESET needed, safe for pooled connections).
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                await _context.Database.ExecuteSqlRawAsync("SET LOCAL enable_nestloop = off; SET LOCAL work_mem = '80MB';", cancellationToken);

                var result = await _context
                    .Verzendingen.AsNoTracking()
                    .Where(rsinFilter)
                    .Where(filter)
                    .Join(
                        _context.EnkelvoudigInformatieObjecten,
                        v => v.InformatieObjectId,
                        e => e.Id,
                        (v, e) => new { e.InformatieObjectType, e.LatestEnkelvoudigInformatieObjectVersieId }
                    )
                    .Join(
                        _context.EnkelvoudigInformatieObjectVersies.Where(ver => ver.Owner == _rsin),
                        ve => ve.LatestEnkelvoudigInformatieObjectVersieId,
                        ver => ver.Id,
                        (ve, ver) => new { ve.InformatieObjectType, ver.Vertrouwelijkheidaanduiding }
                    )
                    .Where(ev =>
                        _context.TempInformatieObjectAuthorization.Any(a =>
                            a.InformatieObjectType == ev.InformatieObjectType
                            && (int)ev.Vertrouwelijkheidaanduiding <= a.MaximumVertrouwelijkheidAanduiding
                        )
                    )
                    .CountAsync(cancellationToken);

                return result;
            },
            absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(5),
            cancellationToken
        );

        return totalCount;
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
