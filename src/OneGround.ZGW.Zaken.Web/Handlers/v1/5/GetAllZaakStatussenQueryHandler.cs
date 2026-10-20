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
        IZaakAuthorizationTempTableService zaakAuthorizationTempTableService,
        IZaakKenmerkenResolver zaakKenmerkenResolver
    )
        : base(logger, configuration, authorizationContextAccessor, uriService, zaakKenmerkenResolver)
    {
        _context = context;
        _zaakAuthorizationTempTableService = zaakAuthorizationTempTableService;
    }

    public async Task<QueryResult<PagedResult<ZaakStatus>>> Handle(GetAllZaakStatussenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakStatussen....");

        var filter = GetZaakStatusFilterPredicate(request.GetAllZaakStatussenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakStatus>();

        var query = _context.ZaakStatussen.AsNoTracking().Where(rsinFilter).Where(filter);

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

        // Set the zaak-status zaakinformatieobjecten collection (only those belonging to the status, one query for the whole page)
        var statusIds = pagedResult.Select(z => (Guid?)z.Id).ToList();

        var zioBelongingToStatus = (
            await _context.ZaakInformatieObjecten.AsNoTracking().Where(z => statusIds.Contains(z.StatusId)).ToListAsync(cancellationToken)
        ).ToLookup(z => z.StatusId);

        foreach (var zaakStatus in pagedResult)
        {
            // Note: like GetZaakStatusQueryHandler also on the zaak of the status (the validators do not enforce that a ZAAKINFORMATIEOBJECT's
            // status belongs to the same ZAAK). Every status needs its own Zaak instance here, which AsNoTracking gives (no identity resolution): with
            // AsNoTrackingWithIdentityResolution or a tracking query the statuses of one zaak would share it and overwrite each other's collection.
            zaakStatus.Zaak.ZaakInformatieObjecten = zioBelongingToStatus[zaakStatus.Id].Where(z => z.ZaakId == zaakStatus.ZaakId).ToList();
        }

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
