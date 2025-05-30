using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Catalogi.Web.Services;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class GetAllStatusTypenQueryHandler
    : CatalogiBaseHandler<GetAllStatusTypenQueryHandler>,
        IRequestHandler<GetAllStatusTypenQuery, QueryResult<PagedResult<StatusType>>>
{
    private readonly IEindStatusResolver _eindStatusResolver;
    private readonly ZtcDbContext _context;

    public GetAllStatusTypenQueryHandler(
        ILogger<GetAllStatusTypenQueryHandler> logger,
        IConfiguration configuration,
        IEindStatusResolver eindStatusResolver,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _eindStatusResolver = eindStatusResolver;
        _context = context;
    }

    public async Task<QueryResult<PagedResult<StatusType>>> Handle(GetAllStatusTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all StatusTypen....");

        var filter = GetStatusTypenFilterPredicate(request.GetAllStatusTypenFilter);

        var rsinFilter = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.StatusTypen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        await _eindStatusResolver.ResolveAsync(pagedResult, cancellationToken);

        var result = new PagedResult<StatusType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<StatusType>>(result, QueryStatus.OK);
    }

    private Expression<Func<StatusType, bool>> GetStatusTypenFilterPredicate(GetAllStatusTypenFilter filter)
    {
        return z =>
            (
                filter.Status == ConceptStatus.concept && z.ZaakType.Concept == true
                || filter.Status == ConceptStatus.definitief && z.ZaakType.Concept == false
                || filter.Status == ConceptStatus.alles
            ) && (filter.ZaakType == null || z.ZaakType.Id == _uriService.GetId(filter.ZaakType));
    }
}

class GetAllStatusTypenQuery : IRequest<QueryResult<PagedResult<StatusType>>>
{
    public GetAllStatusTypenFilter GetAllStatusTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
