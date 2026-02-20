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

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _informatieObjectAuthorizationTempTableService.InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            query = query
                .Join(
                    _context.TempInformatieObjectAuthorization,
                    o => o.InformatieObjectType,
                    i => i.InformatieObjectType,
                    (i, a) => new { InformatieObject = i, Authorisatie = a }
                )
                .Where(i =>
                    (int)i.InformatieObject.LatestEnkelvoudigInformatieObjectVersie.Vertrouwelijkheidaanduiding
                    <= i.Authorisatie.MaximumVertrouwelijkheidAanduiding
                )
                .Select(i => i.InformatieObject);
        }

        var totalCount = await GetTotalCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken);

        var pagedResult = await query
            .Include(e => e.LatestEnkelvoudigInformatieObjectVersie)
            .ThenInclude(e => e.BestandsDelen)
            .OrderBy(e => e.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

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

    private static Expression<Func<EnkelvoudigInformatieObject, bool>> GetEnkelvoudigInformatieObjectFilterPredicate(
        GetAllEnkelvoudigInformatieObjectenFilter filter
    )
    {
        var filterUuid_In = filter.Uuid_In?.Select(u => u.ToLower()).ToList();

        return e =>
            (filter.Bronorganisatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Bronorganisatie == filter.Bronorganisatie)
            && (filter.Identificatie == null || e.LatestEnkelvoudigInformatieObjectVersie.Identificatie == filter.Identificatie)
            && (filterUuid_In == null || filterUuid_In.Contains(e.Id.ToString()))
            && (
                filter.Trefwoorden_In == null
                || e.LatestEnkelvoudigInformatieObjectVersie.Trefwoorden.Any(e => filter.Trefwoorden_In.Any(f => f == e))
            );
    }
}

class GetAllEnkelvoudigInformatieObjectenQuery : IRequest<QueryResult<PagedResult<EnkelvoudigInformatieObject>>>
{
    public GetAllEnkelvoudigInformatieObjectenFilter GetAllEnkelvoudigInformatieObjectenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal init; }
}
