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

class GetZaakStatusQueryHandler : ZakenBaseHandler<GetZaakStatusQueryHandler>, IRequestHandler<GetZaakStatusQuery, QueryResult<ZaakStatus>>
{
    private readonly ZrcDbContext _context;

    public GetZaakStatusQueryHandler(
        ILogger<GetZaakStatusQueryHandler> logger,
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

    public async Task<QueryResult<ZaakStatus>> Handle(GetZaakStatusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakStatus {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakStatus>();

        var zaakStatus = await _context
            .ZaakStatussen.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakStatus == null)
        {
            return new QueryResult<ZaakStatus>(null, QueryStatus.NotFound);
        }

        if (
            !_authorizationContext.IsAuthorized(zaakStatus.Zaak.Zaaktype, zaakStatus.Zaak.VertrouwelijkheidAanduiding, AuthorizationScopes.Zaken.Read)
        )
        {
            return new QueryResult<ZaakStatus>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakStatus>(zaakStatus, QueryStatus.OK);
    }
}

class GetZaakStatusQuery : IRequest<QueryResult<ZaakStatus>>
{
    public Guid Id { get; set; }
}
