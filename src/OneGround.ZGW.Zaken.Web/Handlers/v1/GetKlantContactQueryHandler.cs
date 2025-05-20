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
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetKlantContactQueryHandler : ZakenBaseHandler<GetKlantContactQueryHandler>, IRequestHandler<GetKlantContactQuery, QueryResult<KlantContact>>
{
    private readonly ZrcDbContext _context;

    public GetKlantContactQueryHandler(
        ILogger<GetKlantContactQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<KlantContact>> Handle(GetKlantContactQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get KlantContacten: {Id} ", request.Id);

        var rsinFilter = GetRsinFilterPredicate<KlantContact>(z => z.Zaak.Owner == _rsin);

        var result = await _context
            .KlantContacten.AsNoTracking()
            .Where(rsinFilter)
            .Include(k => k.Zaak)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (result == null)
        {
            return new QueryResult<KlantContact>(null, QueryStatus.NotFound);
        }

        return new QueryResult<KlantContact>(result, QueryStatus.OK);
    }
}

public class GetKlantContactQuery : IRequest<QueryResult<KlantContact>>
{
    public Guid Id { get; set; }
}
