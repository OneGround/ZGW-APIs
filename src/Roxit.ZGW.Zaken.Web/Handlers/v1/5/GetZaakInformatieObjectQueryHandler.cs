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

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._5;

class GetZaakInformatieObjectQueryHandler
    : ZakenBaseHandler<GetZaakInformatieObjectQueryHandler>,
        IRequestHandler<GetZaakInformatieObjectQuery, QueryResult<ZaakInformatieObject>>
{
    private readonly ZrcDbContext _context;

    public GetZaakInformatieObjectQueryHandler(
        ILogger<GetZaakInformatieObjectQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService: null)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakInformatieObject>> Handle(GetZaakInformatieObjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakInformatieObject>();

        var zaakstatus = await _context
            .ZaakInformatieObjecten.AsNoTracking()
            .Include(z => z.Status)
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakstatus == null)
        {
            return new QueryResult<ZaakInformatieObject>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ZaakInformatieObject>(zaakstatus, QueryStatus.OK);
    }
}

class GetZaakInformatieObjectQuery : IRequest<QueryResult<ZaakInformatieObject>>
{
    public Guid Id { get; set; }
}
