using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using OneGround.ZGW.Catalogi.Web.Services;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetAllStatusTypenQueryHandler
    : CatalogiBaseHandler<GetAllStatusTypenQueryHandler>,
        IRequestHandler<GetAllStatusTypenQuery, QueryResult<PagedResult<StatusType>>>
{
    private readonly IEindStatusResolver _eindStatusResolver;
    private readonly ZtcDbContext _context;

    public GetAllStatusTypenQueryHandler(
        ILogger<GetAllStatusTypenQueryHandler> logger,
        IConfiguration configuration,
        IEindStatusResolver eindStatusResolver,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _eindStatusResolver = eindStatusResolver;
        _context = context;
    }

    public async Task<QueryResult<PagedResult<StatusType>>> Handle(GetAllStatusTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all StatusTypen....");

        var rsinFilter = GetRsinFilterPredicate<StatusType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.StatusTypen.AsNoTracking().Where(rsinFilter).Where(request.GetAllStatusTypenFilter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType.Catalogus)
            .Include(s => s.StatusTypeVerplichteEigenschappen)
            .ThenInclude(s => s.Eigenschap)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        await _eindStatusResolver.ResolveAsync(pagedResult, cancellationToken);

        var result = new PagedResult<StatusType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<StatusType>>(result, QueryStatus.OK);
    }
}

internal static class IStatusTypeQueryableExtension
{
    public static IQueryable<StatusType> Where(this IQueryable<StatusType> statustypen, GetAllStatusTypenFilter filter)
    {
        Guid zaakTypeId = default;
        if (filter.ZaakType != null)
        {
            zaakTypeId = Guid.Parse(filter.ZaakType.Split('/').Last());
        }

        return statustypen
            .WhereIf(
                filter.Status != ConceptStatus.alles,
                r =>
                    filter.Status == ConceptStatus.concept && r.ZaakType.Concept == true
                    || filter.Status == ConceptStatus.definitief && r.ZaakType.Concept == false
            )
            .WhereIf(filter.ZaakType != null, r => r.ZaakType.Id == zaakTypeId)
            .WhereIf(filter.ZaaktypeIdentificatie != null, r => r.ZaakType.Identificatie == filter.ZaaktypeIdentificatie)
            .WhereIf(
                filter.DatumGeldigheid.HasValue,
                z =>
                    filter.DatumGeldigheid.Value >= z.BeginGeldigheid && !z.EindeGeldigheid.HasValue
                    || filter.DatumGeldigheid.Value >= z.BeginGeldigheid && filter.DatumGeldigheid.Value <= z.EindeGeldigheid.Value
            );
    }
}

class GetAllStatusTypenQuery : IRequest<QueryResult<PagedResult<StatusType>>>
{
    public GetAllStatusTypenFilter GetAllStatusTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
