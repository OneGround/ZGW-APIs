using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Web.Handlers;

class GetAllAbonnementenQueryHandler : ZGWBaseHandler, IRequestHandler<GetAllAbonnementenQuery, QueryResult<IReadOnlyList<Abonnement>>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<GetAllAbonnementenQueryHandler> _logger;

    public GetAllAbonnementenQueryHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<GetAllAbonnementenQueryHandler> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QueryResult<IReadOnlyList<Abonnement>>> Handle(GetAllAbonnementenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Abonnementen....");

        var rsinFilter = GetRsinFilterPredicate<Abonnement>();

        var result = await _context
            .Abonnementen.AsNoTracking()
            .Where(rsinFilter)
            .Include(a => a.AbonnementKanalen)
            .ThenInclude(a => a.Kanaal)
            .Include(a => a.AbonnementKanalen)
            .ThenInclude(a => a.Filters)
            .ToListAsync(cancellationToken);

        return new QueryResult<IReadOnlyList<Abonnement>>(result, QueryStatus.OK);
    }
}

class GetAllAbonnementenQuery : IRequest<QueryResult<IReadOnlyList<Abonnement>>> { }
