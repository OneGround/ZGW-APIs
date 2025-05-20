using System;
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
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetAllKlantContactenQueryHandler
    : ZakenBaseHandler<GetAllKlantContactenQueryHandler>,
        IRequestHandler<GetAllKlantContactenQuery, QueryResult<PagedResult<KlantContact>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllKlantContactenQueryHandler(
        ILogger<GetAllKlantContactenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZrcDbContext zrcDbContext,
        IAuthorizationContextAccessor authorizationContextAccessor,
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService
    )
        : base(logger, configuration, authorizationContextAccessor, uriService)
    {
        _context = zrcDbContext;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<KlantContact>>> Handle(GetAllKlantContactenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all KlantContacten....");

        var filter = GetKlantContactFilterPredicate(request.GetAllKlantContactenFilter);

        var rsinFilter = GetRsinFilterPredicate<KlantContact>(z => z.Zaak.Owner == _rsin);

        var query = _context.KlantContacten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { KlantContacten = r, Authorisatie = a })
                .Where(r => (int)r.KlantContacten.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.KlantContacten);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(z => z.Zaak)
            .OrderBy(z => z.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<KlantContact> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<KlantContact>>(result, QueryStatus.OK);
    }

    private Expression<Func<KlantContact, bool>> GetKlantContactFilterPredicate(GetAllKlantContactenFilter filter)
    {
        return z => filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak);
    }
}

public class GetAllKlantContactenQuery : IRequest<QueryResult<PagedResult<KlantContact>>>
{
    public GetAllKlantContactenFilter GetAllKlantContactenFilter { get; set; }
    public PaginationFilter Pagination { get; internal set; }
}
