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

class GetZaakVerzoekQueryHandler : ZakenBaseHandler<GetZaakVerzoekQueryHandler>, IRequestHandler<GetZaakVerzoekQuery, QueryResult<ZaakVerzoek>>
{
    private readonly ZrcDbContext _context;

    public GetZaakVerzoekQueryHandler(
        ILogger<GetZaakVerzoekQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakVerzoek>> Handle(GetZaakVerzoekQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakVerzoek {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakVerzoek>();

        var zaakverzoek = await _context
            .ZaakVerzoeken.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakverzoek == null)
        {
            return new QueryResult<ZaakVerzoek>(null, QueryStatus.NotFound);
        }

        if (
            !_authorizationContext.IsAuthorized(
                zaakverzoek.Zaak.Zaaktype,
                zaakverzoek.Zaak.VertrouwelijkheidAanduiding,
                AuthorizationScopes.Zaken.Read
            )
        )
        {
            return new QueryResult<ZaakVerzoek>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakVerzoek>(zaakverzoek, QueryStatus.OK);
    }
}

class GetZaakVerzoekQuery : IRequest<QueryResult<ZaakVerzoek>>
{
    public Guid Id { get; set; }
}
