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

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

class GetGebruiksRechtQueryHandler
    : DocumentenBaseHandler<GetGebruiksRechtQueryHandler>,
        IRequestHandler<GetGebruiksRechtQuery, QueryResult<GebruiksRecht>>
{
    private readonly DrcDbContext _context;

    public GetGebruiksRechtQueryHandler(
        ILogger<GetGebruiksRechtQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<GebruiksRecht>> Handle(GetGebruiksRechtQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get GebruiksRecht {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<GebruiksRecht>(o => o.InformatieObject.Owner == _rsin);

        var gebruiksrecht = await _context
            .GebruiksRechten.AsNoTracking()
            .Include(v => v.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (gebruiksrecht == null)
        {
            return new QueryResult<GebruiksRecht>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(gebruiksrecht.InformatieObject, AuthorizationScopes.Documenten.Read))
        {
            return new QueryResult<GebruiksRecht>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<GebruiksRecht>(gebruiksrecht, QueryStatus.OK);
    }
}

class GetGebruiksRechtQuery : IRequest<QueryResult<GebruiksRecht>>
{
    public Guid Id { get; set; }
}
