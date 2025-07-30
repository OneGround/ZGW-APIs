using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class GetAllZaakContactmomentenQueryHandler
    : ZakenBaseHandler<GetAllZaakContactmomentenQueryHandler>,
        IRequestHandler<GetAllZaakContactmomentenQuery, QueryResult<IList<ZaakContactmoment>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakContactmomentenQueryHandler(
        ILogger<GetAllZaakContactmomentenQueryHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<IList<ZaakContactmoment>>> Handle(GetAllZaakContactmomentenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Zaakcontactmomenten....");

        var filter = GetZaakContactmomentFilterPredicate(request.GetAllZaakContactmomentenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakContactmoment>();

        var query = _context.ZaakContactmomenten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(
                    _context.TempZaakAuthorization,
                    o => o.Zaak.Zaaktype,
                    i => i.ZaakType,
                    (r, a) => new { ZaakContactmoment = r, Authorisatie = a }
                )
                .Where(r => (int)r.ZaakContactmoment.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakContactmoment);
        }

        var result = await query.Include(z => z.Zaak).OrderBy(z => z.Id).ToListAsync(cancellationToken);

        return new QueryResult<IList<ZaakContactmoment>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakContactmoment, bool>> GetZaakContactmomentFilterPredicate(GetAllZaakContactmomentenFilter filter)
    {
        return z =>
            (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak))
            && (filter.Contactmoment == null || z.Contactmoment == filter.Contactmoment);
    }
}

class GetAllZaakContactmomentenQuery : IRequest<QueryResult<IList<ZaakContactmoment>>>
{
    public GetAllZaakContactmomentenFilter GetAllZaakContactmomentenFilter { get; internal set; }
}
