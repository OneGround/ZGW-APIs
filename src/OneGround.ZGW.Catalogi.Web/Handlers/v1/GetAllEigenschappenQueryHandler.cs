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

class GetAllEigenschappenQueryHandler
    : CatalogiBaseHandler<GetAllEigenschappenQueryHandler>,
        IRequestHandler<GetAllEigenschappenQuery, QueryResult<PagedResult<Eigenschap>>>
{
    private readonly ZtcDbContext _context;

    public GetAllEigenschappenQueryHandler(
        ILogger<GetAllEigenschappenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<Eigenschap>>> Handle(GetAllEigenschappenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ResultTypen....");

        var filter = GetEigenschappenFilterPredicate(request.GetAllEigenschappenFilter);

        var rsinFilter = GetRsinFilterPredicate<Eigenschap>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.Eigenschappen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType)
            .Include(s => s.Specificatie)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Eigenschap> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Eigenschap>>(result, QueryStatus.OK);
    }

    private Expression<Func<Eigenschap, bool>> GetEigenschappenFilterPredicate(GetAllEigenschappenFilter filter)
    {
        return z =>
            (
                filter.Status == ConceptStatus.concept && z.ZaakType.Concept == true
                || filter.Status == ConceptStatus.definitief && z.ZaakType.Concept == false
                || filter.Status == ConceptStatus.alles
            ) && (filter.ZaakType == null || z.ZaakType.Id == _uriService.GetId(filter.ZaakType));
    }
}

class GetAllEigenschappenQuery : IRequest<QueryResult<PagedResult<Eigenschap>>>
{
    public GetAllEigenschappenFilter GetAllEigenschappenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
