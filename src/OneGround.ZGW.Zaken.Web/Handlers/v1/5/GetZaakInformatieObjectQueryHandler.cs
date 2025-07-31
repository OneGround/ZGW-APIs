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

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

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
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, notificatieService: null, zaakKenmerkenResolver)
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
