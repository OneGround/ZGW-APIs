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

class GetZaakResultaatQueryHandler
    : ZakenBaseHandler<GetZaakResultaatQueryHandler>,
        IRequestHandler<GetZaakResultaatQuery, QueryResult<ZaakResultaat>>
{
    private readonly ZrcDbContext _context;

    public GetZaakResultaatQueryHandler(
        ILogger<GetZaakResultaatQueryHandler> logger,
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

    public async Task<QueryResult<ZaakResultaat>> Handle(GetZaakResultaatQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakResultaat {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakResultaat>();

        var zaakresultaat = await _context
            .ZaakResultaten.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakresultaat == null)
        {
            return new QueryResult<ZaakResultaat>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakresultaat.Zaak))
        {
            return new QueryResult<ZaakResultaat>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakResultaat>(zaakresultaat, QueryStatus.OK);
    }
}

class GetZaakResultaatQuery : IRequest<QueryResult<ZaakResultaat>>
{
    public Guid Id { get; set; }
}
