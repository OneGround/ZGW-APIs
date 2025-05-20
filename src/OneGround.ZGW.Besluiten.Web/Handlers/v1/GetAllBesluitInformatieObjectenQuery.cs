using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.Models.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class GetAllBesluitInformatieObjectenQueryHandler
    : BesluitenBaseHandler<GetAllBesluitInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllBesluitInformatieObjectenQuery, QueryResult<IList<BesluitInformatieObject>>>
{
    private readonly BrcDbContext _context;

    public GetAllBesluitInformatieObjectenQueryHandler(
        ILogger<GetAllBesluitInformatieObjectenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<IList<BesluitInformatieObject>>> Handle(
        GetAllBesluitInformatieObjectenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all BesluitInformatieObjecten....");

        var filter = GetBesluitInformatieObjectFilterPredicate(request.GetAllBesluitInformatieObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<BesluitInformatieObject>(o => o.Besluit.Owner == _rsin);

        var result = await _context
            .BesluitInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .Where(filter)
            .Include(z => z.Besluit)
            .ToListAsync(cancellationToken);

        return new QueryResult<IList<BesluitInformatieObject>>(result, QueryStatus.OK);
    }

    private Expression<Func<BesluitInformatieObject, bool>> GetBesluitInformatieObjectFilterPredicate(GetAllBesluitInformatieObjectenFilter filter)
    {
        return z =>
            (filter.Besluit == null || z.Besluit.Id == _uriService.GetId(filter.Besluit))
            && (filter.InformatieObject == null || z.InformatieObject == filter.InformatieObject);
    }
}

class GetAllBesluitInformatieObjectenQuery : IRequest<QueryResult<IList<BesluitInformatieObject>>>
{
    public GetAllBesluitInformatieObjectenFilter GetAllBesluitInformatieObjectenFilter { get; internal set; }
}
