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
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.Zaken.DataModel;
using Roxit.ZGW.Zaken.Web.Models.v1._5;
using Roxit.ZGW.Zaken.Web.Services;

namespace Roxit.ZGW.Zaken.Web.Handlers.v1._5;

class GetAllZaakVerzoekenQueryHandler
    : ZakenBaseHandler<GetAllZaakVerzoekenQueryHandler>,
        IRequestHandler<GetAllZaakVerzoekenQuery, QueryResult<IList<ZaakVerzoek>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakVerzoekenQueryHandler(
        ILogger<GetAllZaakVerzoekenQueryHandler> logger,
        IConfiguration configuration,
        ZrcDbContext context,
        IEntityUriService uriService,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<IList<ZaakVerzoek>>> Handle(GetAllZaakVerzoekenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Zaakverzoeken....");

        var filter = GetZaakVerzoekFilterPredicate(request.GetAllZaakVerzoekenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakVerzoek>();

        var query = _context.ZaakVerzoeken.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { ZaakVerzoek = r, Authorisatie = a })
                .Where(r => (int)r.ZaakVerzoek.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakVerzoek);
        }

        var result = await query.Include(z => z.Zaak).OrderBy(z => z.Id).ToListAsync(cancellationToken);

        return new QueryResult<IList<ZaakVerzoek>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakVerzoek, bool>> GetZaakVerzoekFilterPredicate(GetAllZaakVerzoekenFilter filter)
    {
        return z => (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak)) && (filter.Verzoek == null || z.Verzoek == filter.Verzoek);
    }
}

class GetAllZaakVerzoekenQuery : IRequest<QueryResult<IList<ZaakVerzoek>>>
{
    public GetAllZaakVerzoekenFilter GetAllZaakVerzoekenFilter { get; internal set; }
}
