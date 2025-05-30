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

class GetZaakStatusQueryHandler : ZakenBaseHandler<GetZaakStatusQueryHandler>, IRequestHandler<GetZaakStatusQuery, QueryResult<ZaakStatus>>
{
    private readonly ZrcDbContext _context;

    public GetZaakStatusQueryHandler(
        ILogger<GetZaakStatusQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
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
