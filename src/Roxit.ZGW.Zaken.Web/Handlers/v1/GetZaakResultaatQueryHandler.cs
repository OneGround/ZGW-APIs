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
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
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
