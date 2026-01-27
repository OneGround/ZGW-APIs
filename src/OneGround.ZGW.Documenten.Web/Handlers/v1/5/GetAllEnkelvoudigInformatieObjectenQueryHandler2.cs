using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Caching;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Messaging.Filters;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1._5;
using OneGround.ZGW.Documenten.Web.Services;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._5;

class GetAllEnkelvoudigInformatieObjectenQueryHandler2
    : DocumentenBaseHandler<GetAllEnkelvoudigInformatieObjectenQueryHandler2>,
        IRequestHandler<GetAllEnkelvoudigInformatieObjectenQuery2, QueryResult<PagedResult<EnkelvoudigInformatieObjectVersie>>>
{
    private readonly DrcDbContext _context;
    private readonly IDistributedCacheHelper _cache;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    public GetAllEnkelvoudigInformatieObjectenQueryHandler2(
        ILogger<GetAllEnkelvoudigInformatieObjectenQueryHandler2> logger,
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

    public async Task<QueryResult<PagedResult<EnkelvoudigInformatieObjectVersie>>> Handle(
        GetAllEnkelvoudigInformatieObjectenQuery2 request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all EnkelvoudigInformatieObjecten (latest versions)....");

        var filter = GetEnkelvoudigInformatieObjectFilterPredicate(request.GetAllEnkelvoudigInformatieObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<EnkelvoudigInformatieObjectVersie>();

        var query = _context
            .EnkelvoudigInformatieObjectVersies.FromSqlRaw(
                // Note: The 'DISTINCT ON (enkelvoudiginformatieobject_id)' approach did perform well on PROD / but longer OFFSETs takes significant longer (first findings)
                @"
                    SELECT DISTINCT ON (enkelvoudiginformatieobject_id) *
                    FROM enkelvoudiginformatieobjectversies
                    WHERE owner = @owner
                    ORDER BY enkelvoudiginformatieobject_id, versie DESC
                ",
                new Npgsql.NpgsqlParameter("@owner", _rsin)
            )
            .AsNoTracking()
            .Where(rsinFilter)
            .Where(filter);
        // Note: CREATE INDEX ON enkelvoudiginformatieobjectversies (enkelvoudiginformatieobject_id, versie DESC);

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
                .Where(i => (int)i.InformatieObject.Vertrouwelijkheidaanduiding <= i.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(i => i.InformatieObject);
        }

        var totalCount = await GetTotalCountCachedAsync(query, request.GetAllEnkelvoudigInformatieObjectenFilter, cancellationToken);

        var pagedResult = await query
            .Include(e => e.BestandsDelen)
            .Include(e => e.InformatieObject) // Note: Needed to map field "locked"
            .OrderBy(e => e.EnkelvoudigInformatieObjectId)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var result = new PagedResult<EnkelvoudigInformatieObjectVersie> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<EnkelvoudigInformatieObjectVersie>>(result, QueryStatus.OK);
    }

    private async Task<int> GetTotalCountCachedAsync(
        IQueryable<EnkelvoudigInformatieObjectVersie> query,
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

    private static Expression<Func<EnkelvoudigInformatieObjectVersie, bool>> GetEnkelvoudigInformatieObjectFilterPredicate(
        GetAllEnkelvoudigInformatieObjectenFilter filter
    )
    {
        var filterUuid_In = filter.Uuid_In?.Select(u => u.ToLower()).ToList();

        return e =>
            (filter.Bronorganisatie == null || e.Bronorganisatie == filter.Bronorganisatie) // Note: Fixed issue not searching in all versions but latest only (GetAll returns latest versions only)
            && (filter.Identificatie == null || e.Identificatie == filter.Identificatie) // Note: Fixed issue not searching in all versions but latest only (GetAll returns latest versions only)
            && (filterUuid_In == null || filterUuid_In.Contains(e.EnkelvoudigInformatieObjectId.ToString()))
            && (filter.Trefwoorden_In == null || e.Trefwoorden.Any(e => filter.Trefwoorden_In.Any(f => f == e))); // Note: Original version searches from Latest version only!
    }
}

class GetAllEnkelvoudigInformatieObjectenQuery2 : IRequest<QueryResult<PagedResult<EnkelvoudigInformatieObjectVersie>>>
{
    public GetAllEnkelvoudigInformatieObjectenFilter GetAllEnkelvoudigInformatieObjectenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal init; }
}


// Old SQL way
//var query = _context
//    .EnkelvoudigInformatieObjectVersies.Where(e =>
//        e.Versie == _context
//            .EnkelvoudigInformatieObjectVersies.Where(sub => sub.EnkelvoudigInformatieObjectId == e.EnkelvoudigInformatieObjectId)
//            .Max(sub => sub.Versie)
//    )
//    .AsNoTracking()
//    .Where(rsinFilter)
//    .Where(filter);

     
// Alternative Raw query
//var query = _context
//    .EnkelvoudigInformatieObjectVersies.FromSqlRaw(
//        @"
//            SELECT * FROM (
//                SELECT *, ROW_NUMBER() OVER (
//                    PARTITION BY enkelvoudiginformatieobject_id
//                    ORDER BY versie DESC
//                ) as rn
//                FROM enkelvoudiginformatieobjectversies
//            ) t
//            WHERE rn = 1
//            AND owner = @owner
//            ORDER BY enkelvoudiginformatieobject_id
//        ",
//        new Npgsql.NpgsqlParameter("@owner", _rsin)
//    )
//    .AsNoTracking()
//    .Where(rsinFilter)
//    .Where(filter);
