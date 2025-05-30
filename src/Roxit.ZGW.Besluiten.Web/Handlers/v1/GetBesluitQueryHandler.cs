using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.Authorization;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Besluiten.Web.Handlers.v1;

class GetBesluitQueryHandler : BesluitenBaseHandler<GetBesluitQueryHandler>, IRequestHandler<GetBesluitQuery, QueryResult<Besluit>>
{
    private readonly BrcDbContext _context;

    public GetBesluitQueryHandler(
        ILogger<GetBesluitQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<Besluit>> Handle(GetBesluitQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Besluit {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Besluit>();

        var besluit = await _context.Besluiten.AsNoTracking().Where(rsinFilter).SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (besluit == null)
        {
            return new QueryResult<Besluit>(null, QueryStatus.NotFound);
        }

        return !_authorizationContext.IsAuthorized(besluit)
            ? new QueryResult<Besluit>(null, QueryStatus.Forbidden)
            : new QueryResult<Besluit>(besluit, QueryStatus.OK);
    }
}

class GetBesluitQuery : IRequest<QueryResult<Besluit>>
{
    public Guid Id { get; set; }
}
