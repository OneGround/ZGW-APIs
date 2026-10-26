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

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._7;

class GetZaakNotitieQueryHandler : ZakenBaseHandler<GetZaakNotitieQueryHandler>, IRequestHandler<GetZaakNotitieQuery, QueryResult<ZaakNotitie>>
{
    private readonly ZrcDbContext _context;

    public GetZaakNotitieQueryHandler(
        ILogger<GetZaakNotitieQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakNotitie>> Handle(GetZaakNotitieQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakNotitie {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakNotitie>();

        var zaakNotitie = await _context
            .ZaakNotities.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakNotitie == null)
        {
            return new QueryResult<ZaakNotitie>(null, QueryStatus.NotFound);
        }

        if (
            !_authorizationContext.IsAuthorized(
                zaakNotitie.Zaak.Zaaktype,
                zaakNotitie.Zaak.VertrouwelijkheidAanduiding,
                AuthorizationScopes.Zaken.Read
            )
        )
        {
            return new QueryResult<ZaakNotitie>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakNotitie>(zaakNotitie, QueryStatus.OK);
    }
}

class GetZaakNotitieQuery : IRequest<QueryResult<ZaakNotitie>>
{
    public Guid Id { get; set; }
}
