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

class GetAllZaakResultatenQueryHandler
    : ZakenBaseHandler<GetAllZaakResultatenQueryHandler>,
        IRequestHandler<GetAllZaakResultatenQuery, QueryResult<PagedResult<ZaakResultaat>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakResultatenQueryHandler(
        ILogger<GetAllZaakResultatenQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<ZaakResultaat>>> Handle(GetAllZaakResultatenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all Resultaten....");

        var filter = GetZaakResultaatFilterPredicate(request.GetAllZaakResultatenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakResultaat>();

        var query = _context.ZaakResultaten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { ZaakResultaat = r, Authorisatie = a })
                .Where(r => (int)r.ZaakResultaat.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakResultaat);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(z => z.Zaak)
            .OrderBy(z => z.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakResultaat> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakResultaat>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakResultaat, bool>> GetZaakResultaatFilterPredicate(GetAllZaakResultatenFilter filter)
    {
        return z =>
            (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak))
            && (filter.ResultaatType == null || z.ResultaatType == filter.ResultaatType);
    }
}

class GetAllZaakResultatenQuery : IRequest<QueryResult<PagedResult<ZaakResultaat>>>
{
    public GetAllZaakResultatenFilter GetAllZaakResultatenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
