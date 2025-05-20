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

class GetAllZaakObjectTypenQueryHandler
    : CatalogiBaseHandler<GetAllZaakObjectTypenQueryHandler>,
        IRequestHandler<GetAllZaakObjectTypenQuery, QueryResult<PagedResult<ZaakObjectType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllZaakObjectTypenQueryHandler(
        ILogger<GetAllZaakObjectTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<ZaakObjectType>>> Handle(GetAllZaakObjectTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ZaakObjectTypen....");

        var rsinFilter = GetRsinFilterPredicate<ZaakObjectType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.ZaakObjectTypen.AsNoTracking().Where(rsinFilter).Where(request.GetAllZaakObjectTypenFilter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .Include(s => s.ZaakType.Catalogus)
            // TODO: We ask VNG how the relations can be edited:
            //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
            //.Include(s => s.ResultaatTypen)
            //.Include(s => s.StatusTypen)
            // ----
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ZaakObjectType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakObjectType>>(result, QueryStatus.OK);
    }
}

internal static class IZaakObjectTypeQueryableExtension
{
    public static IQueryable<ZaakObjectType> Where(this IQueryable<ZaakObjectType> zaakobjecttypen, GetAllZaakObjectTypenFilter filter)
    {
        Guid zaakTypeId = default;
        if (filter.ZaakType != null)
        {
            zaakTypeId = Guid.Parse(filter.ZaakType.Split('/').Last());
        }
        Guid catalogusId = default;
        if (filter.Catalogus != null)
        {
            catalogusId = Guid.Parse(filter.Catalogus.Split('/').Last());
        }

        return zaakobjecttypen
            .WhereIf(filter.ZaakType != null, r => r.ZaakType.Id == zaakTypeId)
            .WhereIf(filter.Catalogus != null, r => r.ZaakType.CatalogusId == catalogusId)
            .WhereIf(filter.ZaaktypeIdentificatie != null, r => r.ZaakType.Identificatie == filter.ZaaktypeIdentificatie)
            .WhereIf(filter.AnderObjectType.HasValue, r => r.AnderObjectType == filter.AnderObjectType.Value)
            .WhereIf(filter.ObjectType != null, r => r.ObjectType == filter.ObjectType)
            .WhereIf(filter.RelatieOmschrijving != null, r => r.RelatieOmschrijving == filter.RelatieOmschrijving)
            .WhereIf(filter.DatumBeginGeldigheid.HasValue, z => filter.DatumBeginGeldigheid.Value >= z.BeginGeldigheid)
            .WhereIf(
                filter.DatumEindeGeldigheid.HasValue,
                z => !z.EindeGeldigheid.HasValue || filter.DatumEindeGeldigheid.Value <= z.EindeGeldigheid.Value
            )
            .WhereIf(
                filter.DatumGeldigheid.HasValue,
                z =>
                    filter.DatumGeldigheid.Value >= z.BeginGeldigheid && !z.EindeGeldigheid.HasValue
                    || filter.DatumGeldigheid.Value >= z.BeginGeldigheid && filter.DatumGeldigheid.Value <= z.EindeGeldigheid.Value
            );
    }
}

class GetAllZaakObjectTypenQuery : IRequest<QueryResult<PagedResult<ZaakObjectType>>>
{
    public GetAllZaakObjectTypenFilter GetAllZaakObjectTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
