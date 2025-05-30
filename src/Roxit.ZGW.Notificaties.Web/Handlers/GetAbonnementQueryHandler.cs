using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Handlers;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Web.Handlers;

class GetAbonnementQueryHandler : ZGWBaseHandler, IRequestHandler<GetAbonnementQuery, QueryResult<Abonnement>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<GetAllAbonnementenQueryHandler> _logger;

    public GetAbonnementQueryHandler(
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

    public async Task<QueryResult<Abonnement>> Handle(GetAbonnementQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Abonnement {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Abonnement>();

        var abonnement = await _context
            .Abonnementen.AsNoTracking()
            .Where(rsinFilter)
            .Include(a => a.AbonnementKanalen)
            .ThenInclude(a => a.Kanaal)
            .Include(a => a.AbonnementKanalen)
            .ThenInclude(a => a.Filters)
            .SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (abonnement == null)
        {
            return new QueryResult<Abonnement>(null, QueryStatus.NotFound);
        }

        return new QueryResult<Abonnement>(abonnement, QueryStatus.OK);
    }
}

class GetAbonnementQuery : IRequest<QueryResult<Abonnement>>
{
    public Guid Id { get; set; }
}
