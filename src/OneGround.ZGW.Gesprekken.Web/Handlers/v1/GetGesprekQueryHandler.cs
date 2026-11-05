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
using OneGround.ZGW.Gesprekken.DataModel;

namespace OneGround.ZGW.Gesprekken.Web.Handlers.v1;

class GetGesprekQueryHandler : GesprekkenBaseHandler<GetGesprekQueryHandler>, IRequestHandler<GetGesprekQuery, QueryResult<Gesprek>>
{
    private readonly GrcDbContext _context;

    public GetGesprekQueryHandler(
        ILogger<GetGesprekQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        GrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<Gesprek>> Handle(GetGesprekQuery request, CancellationToken cancellationToken)
    {
        var gesprek = await _context
            .Gesprekken.AsNoTracking()
            .Where(GetRsinFilterPredicate<Gesprek>())
            .Include(g => g.Deelnames.OrderBy(d => d.AanvangsmomentDeelname))
            .Include(g => g.Bijdragen.OrderBy(b => b.Leveringsmoment))
                .ThenInclude(b => b.Bijlagen)
            .SingleOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        return gesprek == null ? new QueryResult<Gesprek>(null, QueryStatus.NotFound) : new QueryResult<Gesprek>(gesprek, QueryStatus.OK);
    }
}

class GetGesprekQuery : IRequest<QueryResult<Gesprek>>
{
    public Guid Id { get; set; }
}
