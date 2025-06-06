using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetCatalogusQueryHandler : CatalogiBaseHandler<GetCatalogusQueryHandler>, IRequestHandler<GetCatalogusQuery, QueryResult<Catalogus>>
{
    private readonly ZtcDbContext _context;

    public GetCatalogusQueryHandler(
        ILogger<GetCatalogusQueryHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
    }

    public async Task<QueryResult<Catalogus>> Handle(GetCatalogusQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Catalogus {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Catalogus>(b => b.Owner == _rsin);

        var catalogus = await _context
            .Catalogussen.AsNoTracking()
            .AsSplitQuery()
            .Where(rsinFilter)
            .Include(c => c.ZaakTypes)
            .Include(c => c.BesluitTypes)
            .Include(c => c.InformatieObjectTypes)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (catalogus == null)
        {
            return new QueryResult<Catalogus>(null, QueryStatus.NotFound);
        }

        return new QueryResult<Catalogus>(catalogus, QueryStatus.OK);
    }
}

class GetCatalogusQuery : IRequest<QueryResult<Catalogus>>
{
    public Guid Id { get; set; }
}
