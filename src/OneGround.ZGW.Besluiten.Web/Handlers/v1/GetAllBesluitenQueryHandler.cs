using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Models.v1;
using OneGround.ZGW.Besluiten.Web.Services;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class GetAllBesluitenQueryHandler
    : BesluitenBaseHandler<GetAllBesluitenQueryHandler>,
        IRequestHandler<GetAllBesluitenQuery, QueryResult<PagedResult<Besluit>>>
{
    private readonly BrcDbContext _context;
    private readonly IBesluitAuthorizationTempTableService _besluitAuthorizationTempTableService;

    public GetAllBesluitenQueryHandler(
        ILogger<GetAllBesluitenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitAuthorizationTempTableService besluitAuthorizationTempTableService
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
        _besluitAuthorizationTempTableService = besluitAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<Besluit>>> Handle(GetAllBesluitenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Besluiten....");

        var filter = GetBesluitFilterPredicate(request.GetAllBesluitenFilter);
        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var query = _context.Besluiten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _besluitAuthorizationTempTableService.InsertBesluitAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            query = query
                .Join(_context.TempBesluitAuthorization, o => o.BesluitType, i => i.BesluitType, (b, a) => new { Besluit = b, Authorisatie = a })
                .Select(b => b.Besluit);
        }

        var totalCount = await query.CountAsync(filter, cancellationToken);

        var pagedResult = await query
            .OrderBy(b => b.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Besluit> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Besluit>>(result, QueryStatus.OK);
    }

    private static Expression<Func<Besluit, bool>> GetBesluitFilterPredicate(GetAllBesluitenFilter filter)
    {
        return z =>
            (filter.Identificatie == null || z.Identificatie == filter.Identificatie)
            && (filter.VerantwoordelijkeOrganisatie == null || z.VerantwoordelijkeOrganisatie == filter.VerantwoordelijkeOrganisatie)
            && (filter.BesluitType == null || z.BesluitType == filter.BesluitType)
            && (filter.Zaak == null || z.Zaak == filter.Zaak);
    }
}

class GetAllBesluitenQuery : IRequest<QueryResult<PagedResult<Besluit>>>
{
    public GetAllBesluitenFilter GetAllBesluitenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
