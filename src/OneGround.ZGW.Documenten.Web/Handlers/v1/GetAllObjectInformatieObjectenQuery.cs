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
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;
using OneGround.ZGW.Documenten.Web.Services;
using OneGround.ZGW.Zaken.Web.Handlers;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class GetAllObjectInformatieObjectenQueryHandler
    : DocumentenBaseHandler<GetAllObjectInformatieObjectenQueryHandler>,
        IRequestHandler<GetAllObjectInformatieObjectenQuery, QueryResult<IList<ObjectInformatieObject>>>
{
    private readonly DrcDbContext _context;
    private readonly IInformatieObjectAuthorizationTempTableService _informatieObjectAuthorizationTempTableService;

    public GetAllObjectInformatieObjectenQueryHandler(
        ILogger<GetAllObjectInformatieObjectenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IInformatieObjectAuthorizationTempTableService informatieObjectAuthorizationTempTableService,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
        _informatieObjectAuthorizationTempTableService = informatieObjectAuthorizationTempTableService;
    }

    public async Task<QueryResult<IList<ObjectInformatieObject>>> Handle(
        GetAllObjectInformatieObjectenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all ObjectInformatieObjecten....");

        var filter = GetObjectInformatieObjectFilterPredicate(request.GetAllObjectInformatieObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<ObjectInformatieObject>(o => o.InformatieObject.Owner == _rsin);

        var query = _context.ObjectInformatieObjecten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _informatieObjectAuthorizationTempTableService.InsertInformatieObjectTypeAuthorizationsToTempTableAsync(
                _authorizationContext,
                _context,
                cancellationToken
            );

            query = query
                .Join(
                    _context.TempInformatieObjectAuthorization,
                    o => o.InformatieObject.InformatieObjectType,
                    i => i.InformatieObjectType,
                    (i, a) => new { InformatieObject = i, Authorisatie = a }
                )
                .Join(
                    _context.EnkelvoudigInformatieObjectVersies.AsNoTracking(),
                    ea => ea.InformatieObject.InformatieObject.LatestEnkelvoudigInformatieObjectVersieId,
                    e0 => e0.Id,
                    (i, v) =>
                        new
                        {
                            i.InformatieObject,
                            InformatieObjectVersie = v,
                            i.Authorisatie,
                        }
                )
                .Where(i => i.InformatieObject.InformatieObject.LatestEnkelvoudigInformatieObjectVersie.Owner == _rsin)
                .Where(i => (int)i.InformatieObjectVersie.Vertrouwelijkheidaanduiding <= i.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(i => i.InformatieObject);
        }

        var result = await query.Include(z => z.InformatieObject).OrderBy(e => e.Id).AsSplitQuery().ToListAsync(cancellationToken);

        return new QueryResult<IList<ObjectInformatieObject>>(result, QueryStatus.OK);
    }

    private Expression<Func<ObjectInformatieObject, bool>> GetObjectInformatieObjectFilterPredicate(GetAllObjectInformatieObjectenFilter filter)
    {
        return z =>
            (filter.InformatieObject == null || z.InformatieObject.Id == _uriService.GetId(filter.InformatieObject))
            && (filter.Object == null || z.Object == filter.Object);
    }
}

class GetAllObjectInformatieObjectenQuery : IRequest<QueryResult<IList<ObjectInformatieObject>>>
{
    public GetAllObjectInformatieObjectenFilter GetAllObjectInformatieObjectenFilter { get; internal set; }
}
