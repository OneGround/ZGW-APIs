using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Authorization;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

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
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
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
