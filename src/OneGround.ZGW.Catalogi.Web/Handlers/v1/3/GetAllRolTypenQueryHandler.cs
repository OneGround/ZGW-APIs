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
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1._3;

class GetAllRolTypenQueryHandler
    : CatalogiBaseHandler<GetAllRolTypenQueryHandler>,
        IRequestHandler<GetAllRolTypenQuery, QueryResult<PagedResult<RolType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllRolTypenQueryHandler(
        ILogger<GetAllRolTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<RolType>>> Handle(GetAllRolTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all RolTypen....");

        var rsinFilter = GetRsinFilterPredicate<RolType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.RolTypen.AsNoTracking().Where(rsinFilter).Where(request.GetAllRolTypenFilter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType.Catalogus)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<RolType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<RolType>>(result, QueryStatus.OK);
    }
}

internal static class IRolTypeQueryableExtension
{
    public static IQueryable<RolType> Where(this IQueryable<RolType> roltypen, GetAllRolTypenFilter filter)
    {
        Guid zaakTypeId = default;
        if (filter.ZaakType != null)
        {
            zaakTypeId = Guid.Parse(filter.ZaakType.Split('/').Last());
        }

        return roltypen
            .WhereIf(
                filter.Status != ConceptStatus.alles,
                r =>
                    filter.Status == ConceptStatus.concept && r.ZaakType.Concept == true
                    || filter.Status == ConceptStatus.definitief && r.ZaakType.Concept == false
            )
            .WhereIf(filter.ZaakType != null, r => r.ZaakType.Id == zaakTypeId)
            .WhereIf(filter.ZaaktypeIdentificatie != null, r => r.ZaakType.Identificatie == filter.ZaaktypeIdentificatie)
            .WhereIf(filter.OmschrijvingGeneriek.HasValue, r => r.OmschrijvingGeneriek == filter.OmschrijvingGeneriek)
            .WhereIf(
                filter.DatumGeldigheid.HasValue,
                z =>
                    filter.DatumGeldigheid.Value >= z.BeginGeldigheid && !z.EindeGeldigheid.HasValue
                    || filter.DatumGeldigheid.Value >= z.BeginGeldigheid && filter.DatumGeldigheid.Value <= z.EindeGeldigheid.Value
            );
    }
}

class GetAllRolTypenQuery : IRequest<QueryResult<PagedResult<RolType>>>
{
    public GetAllRolTypenFilter GetAllRolTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
