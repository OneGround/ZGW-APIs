using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.Handlers.v1;

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
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<ObjectInformatieObject>> Handle(GetObjectInformatieObjectQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ObjectInformatieObject {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ObjectInformatieObject>(o => o.InformatieObject.Owner == _rsin);

        var objectInformatieObject = await _context
            .ObjectInformatieObjecten.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.InformatieObject)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (objectInformatieObject == null)
        {
            return new QueryResult<ObjectInformatieObject>(null, QueryStatus.NotFound);
        }

        return new QueryResult<ObjectInformatieObject>(objectInformatieObject, QueryStatus.OK);
    }
}

class GetObjectInformatieObjectQuery : IRequest<QueryResult<ObjectInformatieObject>>
{
    public Guid Id { get; set; }
}
