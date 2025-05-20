using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Models;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Autorisaties.Web.Handlers;

class GetAllApplicatiesQueryHandler
    : AutorisatiesBaseHandler<GetAllApplicatiesQueryHandler>,
        IRequestHandler<GetAllApplicatiesQuery, QueryResult<PagedResult<Applicatie>>>
{
    private readonly AcDbContext _context;

    public GetAllApplicatiesQueryHandler(
        INotificatieService notificatieService,
        IEntityUriService uriService,
        IConfiguration configuration,
        ILogger<GetAllApplicatiesQueryHandler> logger,
        IAuthorizationContextAccessor authorizationContextAccessor,
        AcDbContext context
    )
        : base(notificatieService, authorizationContextAccessor, uriService, configuration, logger)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<Applicatie>>> Handle(GetAllApplicatiesQuery request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Applicatie>();

        var filter = GetAllApplicatiesFilterPredicate(request.GetAllApplicatiesFilter);

        var query = _context.Applicaties.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(a => a.Autorisaties)
            .Include(applicatie => applicatie.ClientIds)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<Applicatie> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<Applicatie>>(result, QueryStatus.OK);
    }

    private static Expression<Func<Applicatie, bool>> GetAllApplicatiesFilterPredicate(GetAllApplicatiesFilter filter)
    {
        var uniqueClientIds = filter.ClientIds.Select(c => c.ToLower()).Distinct().ToList();
        return z => (!uniqueClientIds.Any() || z.ClientIds.Any(client => uniqueClientIds.Contains(client.ClientId.ToLower())));
    }
}

class GetAllApplicatiesQuery : IRequest<QueryResult<PagedResult<Applicatie>>>
{
    public GetAllApplicatiesFilter GetAllApplicatiesFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
