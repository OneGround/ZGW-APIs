using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Besluiten.Web.Handlers.v1;

class GetBesluitInformatieObjectQueryHandler
    : BesluitenBaseHandler<GetBesluitInformatieObjectQueryHandler>,
        IRequestHandler<GetBesluitInformatieObjectQuery, QueryResult<BesluitInformatieObject>>
{
    private readonly BrcDbContext _context;

    public GetBesluitInformatieObjectQueryHandler(
        ILogger<GetBesluitInformatieObjectQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<BesluitInformatieObject>> Handle(GetBesluitInformatieObjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get BesluitInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<BesluitInformatieObject>(o => o.Besluit.Owner == _rsin);

        var besluitstatus = await _context
            .BesluitInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Besluit)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        return besluitstatus == null
            ? new QueryResult<BesluitInformatieObject>(null, QueryStatus.NotFound)
            : new QueryResult<BesluitInformatieObject>(besluitstatus, QueryStatus.OK);
    }
}

class GetBesluitInformatieObjectQuery : IRequest<QueryResult<BesluitInformatieObject>>
{
    public Guid Id { get; set; }
}
