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
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class GetAllResultaatTypenQueryHandler
    : CatalogiBaseHandler<GetAllResultaatTypenQueryHandler>,
        IRequestHandler<GetAllResultaatTypenQuery, QueryResult<PagedResult<ResultaatType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllResultaatTypenQueryHandler(
        ILogger<GetAllResultaatTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<ResultaatType>>> Handle(GetAllResultaatTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ResultTypen....");

        var filter = GetResultaatTypenFilterPredicate(request.GetAllResultaatTypenFilter);

        var rsinFilter = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.ResultaatTypen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType)
            .Include(s => s.BronDatumArchiefProcedure)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ResultaatType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ResultaatType>>(result, QueryStatus.OK);
    }

    private Expression<Func<ResultaatType, bool>> GetResultaatTypenFilterPredicate(GetAllResultaatTypenFilter filter)
    {
        return z =>
            (
                filter.Status == ConceptStatus.concept && z.ZaakType.Concept == true
                || filter.Status == ConceptStatus.definitief && z.ZaakType.Concept == false
                || filter.Status == ConceptStatus.alles
            ) && (filter.ZaakType == null || z.ZaakType.Id == _uriService.GetId(filter.ZaakType));
    }
}

class GetAllResultaatTypenQuery : IRequest<QueryResult<PagedResult<ResultaatType>>>
{
    public GetAllResultaatTypenFilter GetAllResultaatTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
