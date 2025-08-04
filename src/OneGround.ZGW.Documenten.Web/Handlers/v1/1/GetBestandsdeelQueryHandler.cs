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

namespace OneGround.ZGW.Documenten.Web.Handlers.v1._1;

class GetBestandsdeelQueryHandler
    : DocumentenBaseHandler<GetBestandsdeelQueryHandler>,
        IRequestHandler<GetBestandsdeelQuery, QueryResult<BestandsDeel>>
{
    private readonly DrcDbContext _context;

    public GetBestandsdeelQueryHandler(
        ILogger<GetBestandsdeelQueryHandler> logger,
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

    public async Task<QueryResult<BestandsDeel>> Handle(GetBestandsdeelQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Bestandsdeel {BestandsdeelId}....", request.BestandsdeelId);

        var rsinFilter = GetRsinFilterPredicate<BestandsDeel>(o => o.EnkelvoudigInformatieObjectVersie.InformatieObject.Owner == _rsin);

        var bestandsdeel = await _context
            .BestandsDelen.Where(rsinFilter)
            .Include(d => d.EnkelvoudigInformatieObjectVersie)
            .ThenInclude(d => d.InformatieObject)
            .SingleOrDefaultAsync(d => d.Id == request.BestandsdeelId, cancellationToken);

        if (bestandsdeel == null)
        {
            return new QueryResult<BestandsDeel>(null, QueryStatus.NotFound);
        }

        return new QueryResult<BestandsDeel>(bestandsdeel, QueryStatus.OK);
    }
}

class GetBestandsdeelQuery : IRequest<QueryResult<BestandsDeel>>
{
    public Guid BestandsdeelId { get; internal set; }
}
