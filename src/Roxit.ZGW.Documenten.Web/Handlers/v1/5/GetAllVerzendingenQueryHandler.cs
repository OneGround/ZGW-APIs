using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Caching;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Models.v1._5;
using Roxit.ZGW.Documenten.Web.Services;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1._5;

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
        IInformatieObjectAuthorizationTempTableService informatieObjectAuthorizationTempTableService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
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
                    o => o.InformatieObject.InformatieObjectType,
                    i => i.InformatieObjectType,
                    (i, a) => new { InformatieObject = i, Authorisatie = a }
                )
                .Join(
                    _context.EnkelvoudigInformatieObjectVersies.AsNoTracking(),
                    ea => ea.InformatieObject.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId,
                    e0 => e0.Id,
                    (i, v) =>
                        new
                        {
                            i.InformatieObject,
                            InformatieObjectVersie = v,
                            i.Authorisatie,
                        }
                )
                .Where(i => i.InformatieObject.InformatieObject.LatestEnkelvoudigInformatieObjectVersie.Owner == _rsin)
                .Where(i => (int)i.InformatieObjectVersie.Vertrouwelijkheidaanduiding <= i.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(i => i.InformatieObject);
        }

        var totalCount = await GetTotalCountCachedAsync(query, request.GetAllVerzendingenFilter, cancellationToken);

        var pagedResult = await query
            .Include(e => e.InformatieObject)
            .OrderBy(e => e.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Verzending> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Verzending>>(result, QueryStatus.OK);
    }

    private async Task<int> GetTotalCountCachedAsync(
        IQueryable<Verzending> query,
        GetAllVerzendingenFilter filter,
        CancellationToken cancellationToken
    )
    {
        // Create a key for the current request+ClientId (uri contains the query-parameters as well)
        var key = ObjectHasher.ComputeSha1Hash(new { ClientId = _rsin, GetAllVerzendingenFilter = filter });

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
