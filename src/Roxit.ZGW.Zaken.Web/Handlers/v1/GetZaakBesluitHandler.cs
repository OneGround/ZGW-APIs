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

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class GetZaakBesluitHandler : ZakenBaseHandler<GetZaakBesluitHandler>, IRequestHandler<GetZaakBesluiten, QueryResult<ZaakBesluit>>
{
    private readonly ZrcDbContext _context;

    public GetZaakBesluitHandler(
        ILogger<GetZaakBesluitHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakBesluit>> Handle(GetZaakBesluiten request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakBesluit: {Besluit} for zaak: {Zaak}....", request.Besluit, request.Zaak);

        var rsinFilter = GetRsinFilterPredicate<ZaakBesluit>(z => z.Zaak.Owner == _rsin);

        var result = await _context
            .ZaakBesluiten.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .Where(z => z.Id == request.Besluit)
            .Where(z => z.ZaakId == request.Zaak)
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new QueryResult<ZaakBesluit>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(result.Zaak))
        {
            return new QueryResult<ZaakBesluit>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakBesluit>(result, QueryStatus.OK);
    }
}

class GetZaakBesluiten : IRequest<QueryResult<ZaakBesluit>>
{
    public Guid Zaak { get; internal set; }
    public Guid Besluit { get; internal set; }
}
