using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

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
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, besluitKenmerkenResolver)
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
