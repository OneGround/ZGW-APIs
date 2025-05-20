using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Handlers;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Web.Handlers;

class GetAllKanalenQueryHandler : ZGWBaseHandler, IRequestHandler<GetAllKanalenQuery, QueryResult<IReadOnlyList<Kanaal>>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<GetAllKanalenQueryHandler> _logger;

    public GetAllKanalenQueryHandler(
        IConfiguration configuration,
        IAuthorizationContextAccessor authorizationContextAccessor,
        NrcDbContext context,
        ILogger<GetAllKanalenQueryHandler> logger
    )
        : base(configuration, authorizationContextAccessor)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QueryResult<IReadOnlyList<Kanaal>>> Handle(GetAllKanalenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Kanalen....");

        var result = await _context.Kanalen.AsNoTracking().Where(k => request.Naam == null || k.Naam == request.Naam).ToListAsync(cancellationToken);

        return new QueryResult<IReadOnlyList<Kanaal>>(result, QueryStatus.OK);
    }
}

class GetAllKanalenQuery : IRequest<QueryResult<IReadOnlyList<Kanaal>>>
{
    public GetAllKanalenQuery(string naam = null)
    {
        Naam = naam;
    }

    public string Naam { get; set; }
}
