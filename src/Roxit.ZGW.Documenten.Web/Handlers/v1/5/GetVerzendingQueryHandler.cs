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
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;
using Roxit.ZGW.Documenten.Web.Authorization;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1._5;

class GetVerzendingQueryHandler : DocumentenBaseHandler<GetVerzendingQueryHandler>, IRequestHandler<GetVerzendingQuery, QueryResult<Verzending>>
{
    private readonly DrcDbContext _context;

    public GetVerzendingQueryHandler(
        ILogger<GetVerzendingQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<Verzending>> Handle(GetVerzendingQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Verzending {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Verzending>(o => o.InformatieObject.Owner == _rsin);

        var verzending = await _context
            .Verzendingen.AsNoTracking()
            .Include(v => v.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (verzending == null)
        {
            return new QueryResult<Verzending>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(verzending.InformatieObject, AuthorizationScopes.Documenten.Read))
        {
            return new QueryResult<Verzending>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<Verzending>(verzending, QueryStatus.OK);
    }
}

class GetVerzendingQuery : IRequest<QueryResult<Verzending>>
{
    public Guid Id { get; set; }
}
