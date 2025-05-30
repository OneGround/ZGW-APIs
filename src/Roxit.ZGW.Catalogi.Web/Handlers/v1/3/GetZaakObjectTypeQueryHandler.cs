using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class GetZaakObjectTypeQueryHandler
    : CatalogiBaseHandler<GetZaakObjectTypeQueryHandler>,
        IRequestHandler<GetZaakObjectTypeQuery, QueryResult<ZaakObjectType>>
{
    private readonly ZtcDbContext _context;

    public GetZaakObjectTypeQueryHandler(
        ILogger<GetZaakObjectTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakObjectType>> Handle(GetZaakObjectTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakObjectType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var zaakObjectType = await _context
            .ZaakObjectTypen.AsNoTracking()
            .Where(rsinFilter)
            .Include(s => s.ZaakType.Catalogus)
            // TODO: We ask VNG how the relations can be edited:
            //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
            //.Include(s => s.ResultaatTypen)
            //.Include(s => s.StatusTypen)
            // ----
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakObjectType == null)
        {
            return new QueryResult<ZaakObjectType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ZaakObjectType>(zaakObjectType, QueryStatus.OK);
    }
}

class GetZaakObjectTypeQuery : IRequest<QueryResult<ZaakObjectType>>
{
    public Guid Id { get; set; }
}
