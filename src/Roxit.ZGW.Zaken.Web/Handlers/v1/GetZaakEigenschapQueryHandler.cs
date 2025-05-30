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

class GetZaakEigenschapQueryHandler
    : ZakenBaseHandler<GetZaakEigenschapQueryHandler>,
        IRequestHandler<GetZaakEigenschapQuery, QueryResult<ZaakEigenschap>>
{
    private readonly ZrcDbContext _context;

    public GetZaakEigenschapQueryHandler(
        ILogger<GetZaakEigenschapQueryHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakEigenschap>> Handle(GetZaakEigenschapQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakEigenschap: {Eigenschap} for zaak: {Zaak}....", request.Eigenschap, request.Zaak);

        var rsinFilter = GetRsinFilterPredicate<ZaakEigenschap>();

        var result = await _context
            .ZaakEigenschappen.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .Where(z => z.Id == request.Eigenschap)
            .Where(z => z.ZaakId == request.Zaak)
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new QueryResult<ZaakEigenschap>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(result.Zaak))
        {
            return new QueryResult<ZaakEigenschap>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakEigenschap>(result, QueryStatus.OK);
    }
}

class GetZaakEigenschapQuery : IRequest<QueryResult<ZaakEigenschap>>
{
    public Guid Zaak { get; internal set; }
    public Guid Eigenschap { get; internal set; }
}
