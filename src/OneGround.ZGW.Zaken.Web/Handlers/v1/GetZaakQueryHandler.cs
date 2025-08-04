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
using OneGround.ZGW.Zaken.Web.Authorization;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetZaakQueryHandler : ZakenBaseHandler<GetZaakQueryHandler>, IRequestHandler<GetZaakQuery, QueryResult<Zaak>>
{
    private readonly ZrcDbContext _context;

    public GetZaakQueryHandler(
        ILogger<GetZaakQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
    }

    public async Task<QueryResult<Zaak>> Handle(GetZaakQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get Zaak {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<Zaak>();

        var zaak = await _context
            .Zaken.AsNoTracking()
            .AsSplitQuery()
            .Where(rsinFilter)
            .Include(z => z.Hoofdzaak)
            .Include(z => z.Deelzaken)
            .Include(z => z.RelevanteAndereZaken)
            .Include(z => z.Kenmerken)
            .Include(z => z.ZaakEigenschappen)
            .Include(z => z.ZaakStatussen)
            .Include(z => z.Resultaat)
            .Include(z => z.Verlenging)
            .Include(z => z.Opschorting)
            .Select(z => new
            {
                Zaak = z,
                ZaakgeometrieConverted = z.Zaakgeometrie != null && request.SRID != 28992
                    ? EF.Functions.Transform(z.Zaakgeometrie, request.SRID)
                    : null,
            })
            .SingleOrDefaultAsync(z => z.Zaak.Id == request.Id, cancellationToken);

        if (zaak == null)
        {
            return new QueryResult<Zaak>(null, QueryStatus.NotFound);
        }

        if (zaak.Zaak.Zaakgeometrie != null && request.SRID != 28992)
        {
            zaak.Zaak.Zaakgeometrie = zaak.ZaakgeometrieConverted;
        }

        if (!_authorizationContext.IsAuthorized(zaak.Zaak))
        {
            return new QueryResult<Zaak>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<Zaak>(zaak.Zaak, QueryStatus.OK);
    }
}

class GetZaakQuery : IRequest<QueryResult<Zaak>>
{
    public Guid Id { get; internal set; }
    public int SRID { get; internal set; } = 28992;
}
