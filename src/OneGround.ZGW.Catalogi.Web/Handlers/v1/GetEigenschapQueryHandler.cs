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
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class GetEigenschapQueryHandler : CatalogiBaseHandler<GetEigenschapQueryHandler>, IRequestHandler<GetEigenschapQuery, QueryResult<Eigenschap>>
{
    private readonly ZtcDbContext _context;

    public GetEigenschapQueryHandler(
        ILogger<GetEigenschapQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<Eigenschap>> Handle(GetEigenschapQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Eigenschap {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Eigenschap>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var eigenschap = await _context
            .Eigenschappen.AsNoTracking()
            .Where(rsinFilter)
            .Include(s => s.ZaakType)
            .Include(s => s.Specificatie)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (eigenschap == null)
        {
            return new QueryResult<Eigenschap>(null, QueryStatus.NotFound);
        }

        return new QueryResult<Eigenschap>(eigenschap, QueryStatus.OK);
    }
}

class GetEigenschapQuery : IRequest<QueryResult<Eigenschap>>
{
    public Guid Id { get; set; }
}
