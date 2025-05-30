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
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.DataModel.ZaakRol;
using Roxit.ZGW.Zaken.Web.Authorization;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._5;

class GetZaakRolQueryHandler : ZakenBaseHandler<GetZaakRolQueryHandler>, IRequestHandler<GetZaakRolQuery, QueryResult<ZaakRol>>
{
    private readonly ZrcDbContext _context;

    public GetZaakRolQueryHandler(
        ILogger<GetZaakRolQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
    }

    public async Task<QueryResult<ZaakRol>> Handle(GetZaakRolQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get ZaakRol {Id}....", request.Id);

        var rsinFilter = GetRsinFilterPredicate<ZaakRol>();

        var zaakrol = await _context
            .ZaakRollen.AsNoTracking()
            .Where(rsinFilter)
            .Include(z => z.Zaak)
            .ThenInclude(z => z.ZaakStatussen)
            .Include(z => z.ContactpersoonRol)
            .Include(z => z.NatuurlijkPersoon.Verblijfsadres)
            .Include(z => z.NatuurlijkPersoon.SubVerblijfBuitenland)
            .Include(z => z.NietNatuurlijkPersoon.SubVerblijfBuitenland)
            .Include(z => z.Vestiging.Verblijfsadres)
            .Include(z => z.Vestiging.SubVerblijfBuitenland)
            .Include(z => z.OrganisatorischeEenheid)
            .Include(z => z.Medewerker)
            .SingleOrDefaultAsync(z => z.Id == request.Id, cancellationToken);

        if (zaakrol == null)
        {
            return new QueryResult<ZaakRol>(null, QueryStatus.NotFound);
        }

        if (!_authorizationContext.IsAuthorized(zaakrol.Zaak.Zaaktype, zaakrol.Zaak.VertrouwelijkheidAanduiding, AuthorizationScopes.Zaken.Read))
        {
            return new QueryResult<ZaakRol>(null, QueryStatus.Forbidden);
        }

        return new QueryResult<ZaakRol>(zaakrol, QueryStatus.OK);
    }
}

class GetZaakRolQuery : IRequest<QueryResult<ZaakRol>>
{
    public Guid Id { get; set; }
}
