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
using Roxit.ZGW.Zaken.DataModel.ZaakObject;
using Roxit.ZGW.Zaken.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1;

class GetZaakObjectQueryHandler : ZakenBaseHandler<GetZaakObjectQueryHandler>, IRequestHandler<GetZaakObjectQuery, QueryResult<ZaakObject>>
{
    private readonly ZrcDbContext _context;

    public GetZaakObjectQueryHandler(
        ILogger<GetZaakObjectQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService: null)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakObject>> Handle(GetZaakObjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakObject {requestId}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakObject>();

        var zaakobject = await _context
            .ZaakObjecten.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .Include(z => z.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Include(z => z.Adres)
            .Include(z => z.Buurt)
            .Include(z => z.Pand)
            .Include(z => z.KadastraleOnroerendeZaak)
            .Include(z => z.Gemeente)
            .Include(z => z.TerreinGebouwdObject)
            .Include(z => z.Overige)
            .Include(z => z.WozWaardeObject.IsVoor.AanduidingWozObject)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakobject == null)
        {
            return new QueryResult<ZaakObject>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakobject.Zaak))
        {
            return new QueryResult<ZaakObject>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakObject>(zaakobject, QueryStatus.OK);
    }
}

class GetZaakObjectQuery : IRequest<QueryResult<ZaakObject>>
{
    public Guid Id { get; set; }
}
