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
using OneGround.ZGW.Zaken.Web.Models.v1._5;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1._5;

class GetAllZaakStatussenQueryHandler
    : ZakenBaseHandler<GetAllZaakStatussenQueryHandler>,
        IRequestHandler<GetAllZaakStatussenQuery, QueryResult<PagedResult<ZaakStatus>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakStatussenQueryHandler(
        ILogger<GetAllZaakStatussenQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<ZaakStatus>>> Handle(GetAllZaakStatussenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakStatussen....");

        var filter = GetZaakStatusFilterPredicate(request.GetAllZaakStatussenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakStatus>();

        var query = _context.ZaakStatussen.AsNoTracking().Include(z => z.Zaak.ZaakInformatieObjecten).Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { ZaakStatus = r, Authorisatie = a })
                .Where(r => (int)r.ZaakStatus.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(z => z.Zaak)
            .OrderBy(z => z.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakStatus> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakStatus>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakStatus, bool>> GetZaakStatusFilterPredicate(GetAllZaakStatussenFilter filter)
    {
        return z =>
            (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak))
            && (filter.StatusType == null || z.StatusType == filter.StatusType)
            && (
                !filter.IndicatieLaatstGezetteStatus.HasValue
                || (z.IndicatieLaatstGezetteStatus != null && z.IndicatieLaatstGezetteStatus == filter.IndicatieLaatstGezetteStatus.Value)
            );
    }
}

class GetAllZaakStatussenQuery : IRequest<QueryResult<PagedResult<ZaakStatus>>>
{
    public GetAllZaakStatussenFilter GetAllZaakStatussenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
