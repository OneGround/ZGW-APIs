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
using OneGround.ZGW.Besluiten.Web.Services;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Besluiten.Web.Handlers.v1;

class GetAllBesluitInformatieObjectenQueryHandler
    : BesluitenBaseHandler<GetAllBesluitInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllBesluitInformatieObjectenQuery, QueryResult<IList<BesluitInformatieObject>>>
{
    private readonly BrcDbContext _context;
    private readonly IBesluitAuthorizationTempTableService _besluitAuthorizationTempTableService;

    public GetAllBesluitInformatieObjectenQueryHandler(
        ILogger<GetAllBesluitInformatieObjectenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        BrcDbContext context,
        IBesluitAuthorizationTempTableService besluitAuthorizationTempTableService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IBesluitKenmerkenResolver besluitKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, besluitKenmerkenResolver)
    {
        _context = context;
        _besluitAuthorizationTempTableService = besluitAuthorizationTempTableService;
    }

    public async Task<QueryResult<IList<BesluitInformatieObject>>> Handle(
        GetAllBesluitInformatieObjectenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all BesluitInformatieObjecten....");

        var filter = GetBesluitInformatieObjectFilterPredicate(request.GetAllBesluitInformatieObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<BesluitInformatieObject>(o => o.Besluit.Owner == _rsin);

        var query = _context.BesluitInformatieObjecten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _besluitAuthorizationTempTableService.InsertBesluitAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            query = query
                .Join(
                    _context.TempBesluitAuthorization,
                    o => o.Besluit.BesluitType,
                    i => i.BesluitType,
                    (r, a) => new { BesluitInformatieObject = r, Authorisatie = a }
                )
                .Select(r => r.BesluitInformatieObject);
        }

        var result = await query.Include(z => z.Besluit).ToListAsync(cancellationToken);

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
