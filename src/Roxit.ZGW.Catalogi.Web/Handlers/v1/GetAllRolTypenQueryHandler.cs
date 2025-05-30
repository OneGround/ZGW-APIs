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
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class GetAllRolTypenQueryHandler
    : CatalogiBaseHandler<GetAllRolTypenQueryHandler>,
        IRequestHandler<GetAllRolTypenQuery, QueryResult<PagedResult<RolType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllRolTypenQueryHandler(
        ILogger<GetAllRolTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<RolType>>> Handle(GetAllRolTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all RolTypen....");

        var filter = GetRolTypenFilterPredicate(request.GetAllRolTypenFilter);

        var rsinFilter = GetRsinFilterPredicate<RolType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.RolTypen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<RolType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<RolType>>(result, QueryStatus.OK);
    }

    private Expression<Func<RolType, bool>> GetRolTypenFilterPredicate(GetAllRolTypenFilter filter)
    {
        return z =>
            (
                filter.Status == ConceptStatus.concept && z.ZaakType.Concept == true
                || filter.Status == ConceptStatus.definitief && z.ZaakType.Concept == false
                || filter.Status == ConceptStatus.alles
            )
            && (!filter.OmschrijvingGeneriek.HasValue || z.OmschrijvingGeneriek == filter.OmschrijvingGeneriek.Value)
            && (filter.ZaakType == null || z.ZaakType.Id == _uriService.GetId(filter.ZaakType));
    }
}

class GetAllRolTypenQuery : IRequest<QueryResult<PagedResult<RolType>>>
{
    public GetAllRolTypenFilter GetAllRolTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
