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
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.Web.Models.v1;
using OneGround.ZGW.Zaken.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Handlers.v1;

class GetAllZaakObjectenQueryHandler
    : ZakenBaseHandler<GetAllZaakObjectenQueryHandler>,
        IRequestHandler<GetAllZaakObjectenQuery, QueryResult<PagedResult<ZaakObject>>>
{
    private readonly ZrcDbContext _context;
    private readonly IZaakAuthorizationTempTableService _zaakAuthorizationTempTableService;

    public GetAllZaakObjectenQueryHandler(
        ILogger<GetAllZaakObjectenQueryHandler> logger,
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

    public async Task<QueryResult<PagedResult<ZaakObject>>> Handle(GetAllZaakObjectenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakObjecten....");

        var filter = GetZaakObjectFilterPredicate(request.GetAllZaakObjectenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakObject>();

        var query = _context.ZaakObjecten.AsNoTracking().Where(rsinFilter).Where(filter);

        if (!_authorizationContext.Authorization.HasAllAuthorizations)
        {
            await _zaakAuthorizationTempTableService.InsertIZaakAuthorizationsToTempTableAsync(_authorizationContext, _context, cancellationToken);

            query = query
                .Join(_context.TempZaakAuthorization, o => o.Zaak.Zaaktype, i => i.ZaakType, (r, a) => new { ZaakObject = r, Authorisatie = a })
                .Where(r => (int)r.ZaakObject.Zaak.VertrouwelijkheidAanduiding <= r.Authorisatie.MaximumVertrouwelijkheidAanduiding)
                .Select(r => r.ZaakObject);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(z => z.Zaak)
            .Include(z => z.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Include(z => z.Adres)
            .Include(z => z.Buurt)
            .Include(z => z.Gemeente)
            .Include(z => z.KadastraleOnroerendeZaak)
            .Include(z => z.Overige)
            .Include(z => z.Pand)
            .Include(z => z.TerreinGebouwdObject)
            .Include(z => z.WozWaardeObject.IsVoor.AanduidingWozObject)
            .OrderBy(z => z.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakObject> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakObject>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakObject, bool>> GetZaakObjectFilterPredicate(GetAllZaakObjectenFilter filter)
    {
        return z =>
            (filter.Zaak == null || z.Zaak.Id == _uriService.GetId(filter.Zaak))
            && (!filter.ObjectType.HasValue || z.ObjectType == filter.ObjectType)
            && (filter.Object == null || z.Object == filter.Object);
    }
}

class GetAllZaakObjectenQuery : IRequest<QueryResult<PagedResult<ZaakObject>>>
{
    public GetAllZaakObjectenFilter GetAllZaakObjectenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
