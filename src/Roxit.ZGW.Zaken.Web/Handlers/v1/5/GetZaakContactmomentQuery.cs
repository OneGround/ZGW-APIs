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
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._5;

class GetZaakContactmomentQueryHandler
    : ZakenBaseHandler<GetZaakContactmomentQueryHandler>,
        IRequestHandler<GetZaakContactmomentQuery, QueryResult<ZaakContactmoment>>
{
    private readonly ZrcDbContext _context;

    public GetZaakContactmomentQueryHandler(
        ILogger<GetZaakContactmomentQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakContactmoment>> Handle(GetZaakContactmomentQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakContactmoment {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakContactmoment>();

        var zaakcontactmoment = await _context
            .ZaakContactmomenten.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakcontactmoment == null)
        {
            return new QueryResult<ZaakContactmoment>(null, QueryStatus.NotFound);
        }

        if (
            !_authorizationContext.IsAuthorized(
                zaakcontactmoment.Zaak.Zaaktype,
                zaakcontactmoment.Zaak.VertrouwelijkheidAanduiding,
                AuthorizationScopes.Zaken.Read
            )
        )
        {
            return new QueryResult<ZaakContactmoment>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakContactmoment>(zaakcontactmoment, QueryStatus.OK);
    }
}

class GetZaakContactmomentQuery : IRequest<QueryResult<ZaakContactmoment>>
{
    public Guid Id { get; set; }
}
