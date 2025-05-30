using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Autorisaties.Web.Handlers;

class GetApplicatieQueryHandler : AutorisatiesBaseHandler<GetApplicatieQueryHandler>, IRequestHandler<GetApplicatieQuery, QueryResult<Applicatie>>
{
    private readonly AcDbContext _context;

    public GetApplicatieQueryHandler(
        INotificatieService notificatieService,
        IEntityUriService uriService,
        IConfiguration configuration,
        ILogger<GetApplicatieQueryHandler> logger,
        IAuthorizationContextAccessor authorizationContextAccessor,
        AcDbContext context
    )
        : base(notificatieService, authorizationContextAccessor, uriService, configuration, logger)
    {
        _context = context;
    }

    public async Task<QueryResult<Applicatie>> Handle(GetApplicatieQuery request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<Applicatie>();

        var filter = GetApplicatieFilterPredicate(request);

        var result = await _context
            .Applicaties.AsNoTracking()
            .Include(z => z.Autorisaties)
            .Include(z => z.ClientIds)
            .Where(rsinFilter)
            .Where(filter)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return new QueryResult<Applicatie>(null, QueryStatus.NotFound);
        }

        return new QueryResult<Applicatie>(result, QueryStatus.OK);
    }

    private static Expression<Func<Applicatie, bool>> GetApplicatieFilterPredicate(GetApplicatieQuery request)
    {
        return z =>
            (request.Id == Guid.Empty || z.Id == request.Id)
            && (request.ClientId == null || z.ClientIds.Any(client => client.ClientId.ToLower() == request.ClientId.ToLower()));
    }
}

class GetApplicatieQuery : IRequest<QueryResult<Applicatie>>
{
    public Guid Id { get; set; }
    public string ClientId { get; set; }
}
