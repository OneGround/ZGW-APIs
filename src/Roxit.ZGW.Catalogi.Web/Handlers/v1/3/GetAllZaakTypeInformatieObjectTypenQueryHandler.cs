using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Models.v1._3;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services;
using Roxit.ZGW.Common.Web.Services.UriServices;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class GetAllZaakTypeInformatieObjectTypenQueryHandler
    : CatalogiBaseHandler<GetAllZaakTypeInformatieObjectTypenQueryHandler>,
        IRequestHandler<GetAllZaakTypeInformatieObjectTypenQuery, QueryResult<PagedResult<ZaakTypeInformatieObjectType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllZaakTypeInformatieObjectTypenQueryHandler(
        ILogger<GetAllZaakTypeInformatieObjectTypenQueryHandler> logger,
        IConfiguration configuration,
        INotificatieService notificatieService,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor, notificatieService)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<ZaakTypeInformatieObjectType>>> Handle(
        GetAllZaakTypeInformatieObjectTypenQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogDebug("Get all ZaakTypeInformatieObjectTypen....");

        var filter = GetZaakTypeInformatieObjectTypen(request.GetAllZaakTypenFilter);

        var rsinFilter = GetRsinFilterPredicate<ZaakTypeInformatieObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.ZaakTypeInformatieObjectTypen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(z => z.ZaakType)
            .ThenInclude(z => z.Catalogus)
            .Include(s => s.StatusType)
            .OrderBy(z => z.ZaakTypeId)
            .ThenBy(z => z.VolgNummer)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakTypeInformatieObjectType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakTypeInformatieObjectType>>(result, QueryStatus.OK);
    }

    private Expression<Func<ZaakTypeInformatieObjectType, bool>> GetZaakTypeInformatieObjectTypen(GetAllZaakTypeInformatieObjectTypenFilter filter)
    {
        return z =>
            (
                filter.Status == ConceptStatus.concept && z.ZaakType.Concept == true
                || filter.Status == ConceptStatus.definitief && z.ZaakType.Concept == false
                || filter.Status == ConceptStatus.alles
            )
            && (filter.ZaakType == null || z.ZaakType.Id == _uriService.GetId(filter.ZaakType))
            && (filter.InformatieObjectType == null || z.InformatieObjectTypeOmschrijving == filter.InformatieObjectType)
            && (filter.Richting == null || z.Richting == filter.Richting);
    }
}

class GetAllZaakTypeInformatieObjectTypenQuery : IRequest<QueryResult<PagedResult<ZaakTypeInformatieObjectType>>>
{
    public GetAllZaakTypeInformatieObjectTypenFilter GetAllZaakTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
