using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetStatusTypeQueryHandler : CatalogiBaseHandler<GetStatusTypeQueryHandler>, IRequestHandler<GetStatusTypeQuery, QueryResult<StatusType>>
{
    private readonly IEindStatusResolver _eindStatusResolver;
    private readonly ZtcDbContext _context;

    public GetStatusTypeQueryHandler(
        ILogger<GetStatusTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        IEindStatusResolver eindStatusResolver,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _eindStatusResolver = eindStatusResolver;
        _context = context;
    }

    public async Task<QueryResult<StatusType>> Handle(GetStatusTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get StatusType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var statusType = await _context
            .StatusTypen.AsNoTracking()
            .Where(rsinFilter)
            .Include(s => s.ZaakType.Catalogus)
            .Include(s => s.StatusTypeVerplichteEigenschappen)
            .ThenInclude(s => s.Eigenschap)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (statusType == null)
        {
            return new QueryResult<StatusType>(null, QueryStatus.NotFound);
        }

        await _eindStatusResolver.ResolveAsync(statusType, cancellationToken);

        return new QueryResult<StatusType>(statusType, QueryStatus.OK);
    }
}

class GetStatusTypeQuery : IRequest<QueryResult<StatusType>>
{
    public Guid Id { get; set; }
}
