using System;
using System.Linq;
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
using OneGround.ZGW.Documenten.Web.Authorization;

namespace OneGround.ZGW.Documenten.Web.Handlers.v1;

class GetObjectInformatieObjectQueryHandler
    : DocumentenBaseHandler<GetObjectInformatieObjectQueryHandler>,
        IRequestHandler<GetObjectInformatieObjectQuery, QueryResult<ObjectInformatieObject>>
{
    private readonly DrcDbContext _context;

    public GetObjectInformatieObjectQueryHandler(
        ILogger<GetObjectInformatieObjectQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        DrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IDocumentKenmerkenResolver documentKenmerkenResolver
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, documentKenmerkenResolver)
    {
        _context = context;
    }

    public async Task<QueryResult<ObjectInformatieObject>> Handle(GetObjectInformatieObjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ObjectInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ObjectInformatieObject>(o => o.InformatieObject.Owner == _rsin);

        var objectInformatieObject = await _context
            .ObjectInformatieObjecten.AsNoTracking()
            .Include(z => z.InformatieObject.LatestEnkelvoudigInformatieObjectVersie)
            .Where(rsinFilter)
            .Include(z => z.InformatieObject)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (objectInformatieObject == null)
        {
            return new QueryResult<ObjectInformatieObject>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(objectInformatieObject.InformatieObject, AuthorizationScopes.Documenten.Read))
        {
            return new QueryResult<ObjectInformatieObject>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ObjectInformatieObject>(objectInformatieObject, QueryStatus.OK);
    }
}

class GetObjectInformatieObjectQuery : IRequest<QueryResult<ObjectInformatieObject>>
{
    public Guid Id { get; set; }
}
