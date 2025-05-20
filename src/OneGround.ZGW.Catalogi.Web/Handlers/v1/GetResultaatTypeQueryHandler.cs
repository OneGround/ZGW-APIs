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

class GetResultaatTypeQueryHandler
    : CatalogiBaseHandler<GetResultaatTypeQueryHandler>,
        IRequestHandler<GetResultaatTypeQuery, QueryResult<ResultaatType>>
{
    private readonly ZtcDbContext _context;

    public GetResultaatTypeQueryHandler(
        ILogger<GetResultaatTypeQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<ResultaatType>> Handle(GetResultaatTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ResultType {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var resultType = await _context
            .ResultaatTypen.AsNoTracking()
            .Where(rsinFilter)
            .Include(s => s.ZaakType)
            .Include(s => s.BronDatumArchiefProcedure)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (resultType == null)
        {
            return new QueryResult<ResultaatType>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ResultaatType>(resultType, QueryStatus.OK);
    }
}

class GetResultaatTypeQuery : IRequest<QueryResult<ResultaatType>>
{
    public Guid Id { get; set; }
}
