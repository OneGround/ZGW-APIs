using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetAllCatalogussenQueryHandler
    : CatalogiBaseHandler<GetAllCatalogussenQueryHandler>,
        IRequestHandler<GetAllCatalogussenQuery, QueryResult<PagedResult<Catalogus>>>
{
    private readonly ZtcDbContext _context;

    public GetAllCatalogussenQueryHandler(
        ILogger<GetAllCatalogussenQueryHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<Catalogus>>> Handle(GetAllCatalogussenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Catalogussen....");

        var filter = GetAllCatalogussenFilterPredicate(request.GetAllCatalogussenFilter);

        var rsinFilter = GetRsinFilterPredicate<Catalogus>();

        var query = _context.Catalogussen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(c => c.ZaakTypes)
            .Include(c => c.BesluitTypes)
            .Include(c => c.InformatieObjectTypes)
            .OrderBy(c => c.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Catalogus> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Catalogus>>(result, QueryStatus.OK);
    }

    private static Expression<Func<Catalogus, bool>> GetAllCatalogussenFilterPredicate(Models.v1.GetAllCatalogussenFilter filter)
    {
        return z =>
            (filter.Domein == null || z.Domein == filter.Domein)
            && (!filter.Domein__in.Any() || filter.Domein__in.Contains(z.Domein))
            && (filter.Rsin == null || z.Rsin == filter.Rsin)
            && (!filter.Rsin__in.Any() || filter.Rsin__in.Contains(z.Rsin));
    }
}

class GetAllCatalogussenQuery : IRequest<QueryResult<PagedResult<Catalogus>>>
{
    public Models.v1.GetAllCatalogussenFilter GetAllCatalogussenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
