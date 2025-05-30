using System;
using System.Collections.Generic;
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

class GetAllZaakBesluitenHandler
    : ZakenBaseHandler<GetAllZaakBesluitenHandler>,
        IRequestHandler<GetAllZaakBesluiten, QueryResult<IEnumerable<ZaakBesluit>>>
{
    private readonly ZrcDbContext _context;

    public GetAllZaakBesluitenHandler(
        ILogger<GetAllZaakBesluitenHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<IEnumerable<ZaakBesluit>>> Handle(GetAllZaakBesluiten request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakBesluiten....");

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.AsNoTracking()
            .Where(rsinFilter)
            .SingleOrDefaultAsync(z => z.Id == request.Zaak, cancellationToken: cancellationToken);

        if (zaak == null)
        {
            return new QueryResult<IEnumerable<ZaakBesluit>>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaak))
        {
            return new QueryResult<IEnumerable<ZaakBesluit>>(null, QueryStatus.Forbidden);
        }

        var result = await _context
            .ZaakBesluiten.AsNoTracking()
            .Include(z => z.Zaak)
            .Where(z => z.ZaakId == request.Zaak)
            .ToListAsync(cancellationToken);

        return new QueryResult<IEnumerable<ZaakBesluit>>(result, QueryStatus.OK);
    }
}

class GetAllZaakBesluiten : IRequest<QueryResult<IEnumerable<ZaakBesluit>>>
{
    public Guid Zaak { get; set; }
}
