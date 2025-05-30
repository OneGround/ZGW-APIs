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

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1;

class GetRolTypeQueryHandler : CatalogiBaseHandler<GetRolTypeQueryHandler>, IRequestHandler<GetRolTypeQuery, QueryResult<RolType>>
{
    private readonly ZtcDbContext _context;

    public GetRolTypeQueryHandler(
        ILogger<GetRolTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<RolType>> Handle(GetRolTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get RolType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<RolType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var rolType = await _context
            .RolTypen.AsNoTracking()
            .Where(rsinFilter)
            .Include(s => s.ZaakType)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (rolType == null)
        {
            return new QueryResult<RolType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<RolType>(rolType, QueryStatus.OK);
    }
}

class GetRolTypeQuery : IRequest<QueryResult<RolType>>
{
    public Guid Id { get; set; }
}
