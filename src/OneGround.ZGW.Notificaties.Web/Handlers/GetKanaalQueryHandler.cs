using System;
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

class GetKanaalQueryHandler : ZGWBaseHandler, IRequestHandler<GetKanaalQuery, QueryResult<Kanaal>>
{
    private readonly NrcDbContext _context;
    private readonly ILogger<GetAllKanalenQueryHandler> _logger;

    public GetKanaalQueryHandler(
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

    public async Task<QueryResult<Kanaal>> Handle(GetKanaalQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Kanaal {Id}....", request.Id);

        var kanaal = await _context.Kanalen.AsNoTracking().SingleOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (kanaal == null)
        {
            return new QueryResult<Kanaal>(null, QueryStatus.NotFound);
        }

        return new QueryResult<Kanaal>(kanaal, QueryStatus.OK);
    }
}

class GetKanaalQuery : IRequest<QueryResult<Kanaal>>
{
    public Guid Id { get; set; }
}
