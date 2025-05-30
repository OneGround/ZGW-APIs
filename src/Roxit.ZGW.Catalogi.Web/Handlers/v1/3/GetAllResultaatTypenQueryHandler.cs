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
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.Extensions;
using Roxit.ZGW.Catalogi.Web.Models.v1._3;
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class GetAllResultaatTypenQueryHandler
    : CatalogiBaseHandler<GetAllResultaatTypenQueryHandler>,
        IRequestHandler<GetAllResultaatTypenQuery, QueryResult<PagedResult<ResultaatType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllResultaatTypenQueryHandler(
        ILogger<GetAllResultaatTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<ResultaatType>>> Handle(GetAllResultaatTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all ResultTypen....");

        var rsinFilter = GetRsinFilterPredicate<ResultaatType>(b => b.ZaakType.Catalogus.Owner == _rsin);

        var query = _context.ResultaatTypen.AsNoTracking().Where(rsinFilter).Where(request.GetAllResultaatTypenFilter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(s => s.ZaakType.Catalogus)
            .Include(s => s.BronDatumArchiefProcedure)
            .Include(s => s.ResultaatTypeBesluitTypen)
            .OrderBy(s => s.Id)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var datumGeldigheid = request.GetAllResultaatTypenFilter.DatumGeldigheid.GetValueOrDefault(DateOnly.FromDateTime(DateTime.Today));

        // Resolve soft Resultaattype-BesluitType relations with the current version within geldigheid
        await ResolveResultaatTypeBesluitTypeRelations(rsinFilter, pagedResult, datumGeldigheid, cancellationToken);

        var result = new PagedResult<ResultaatType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ResultaatType>>(result, QueryStatus.OK);
    }

    private async Task ResolveResultaatTypeBesluitTypeRelations(
        Expression<Func<ResultaatType, bool>> rsinFilter,
        List<ResultaatType> pagedResult,
        DateOnly datumGeldigheid,
        CancellationToken cancellationToken
    )
    {
        // Build lookup resultaattype-besluittypen [M:N] soft JOIN between besluittype.omschrijving within besluittype.geldigheid
        var resultaattypeBesluitTypenLookup = (
            await _context
                .ResultaatTypen.AsNoTracking()
                .Where(rsinFilter)
                .Join(
                    _context.ResultaatTypeBesluitTypen,
                    o => o.Id,
                    i => i.ResultaatTypeId,
                    (z, a) => new { ResultaatType = z, a.BesluitTypeOmschrijving }
                )
                .Join(
                    _context.BesluitTypen.Where(b => !b.Concept),
                    k => k.BesluitTypeOmschrijving,
                    i => i.Omschrijving,
                    (z, b) => new { ResultaatType = z, BesluitType = b }
                )
                .Where(b => b.ResultaatType.ResultaatType.ZaakType.CatalogusId == b.BesluitType.CatalogusId)
                .Where(b =>
                    datumGeldigheid >= b.BesluitType.BeginGeldigheid
                    && (b.BesluitType.EindeGeldigheid == null || datumGeldigheid <= b.BesluitType.EindeGeldigheid)
                )
                .Select(k => new { ResultaatTypeId = k.ResultaatType.ResultaatType.Id, k.BesluitType })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.ResultaatTypeId, v => v.BesluitType);

        // For each resultaattype: Set resultaattype-besluittypen [1:N] soft relations matched on besluittype.omschrijving within besluittype.geldigheid
        foreach (var resultaattype in pagedResult)
        {
            List<ResultaatTypeBesluitType> newResultaatTypeBesluitTypen = [];

            foreach (var resultaattypeBesluittype in resultaattype.ResultaatTypeBesluitTypen)
            {
                if (resultaattypeBesluitTypenLookup.Contains(resultaattype.Id))
                {
                    // Set soft relation between resultaattype and besluittype (is matched on omschrijving within geldigheid)
                    var besluitTypenWithinGeldigheid = resultaattypeBesluitTypenLookup[resultaattype.Id]
                        .Where(b => b.Omschrijving == resultaattypeBesluittype.BesluitTypeOmschrijving);

                    newResultaatTypeBesluitTypen.AddRangeUnique(
                        besluitTypenWithinGeldigheid.Select(b => new ResultaatTypeBesluitType { BesluitType = b }),
                        (x, y) => x.BesluitType.Url == y.BesluitType.Url
                    );
                }
            }
            // Re-map only the valid ResultaatTypeBesluitTypen within geldigheid!!
            resultaattype.ResultaatTypeBesluitTypen = newResultaatTypeBesluitTypen;
        }
    }
}

internal static class IResultaatQueryableExtension
{
    public static IQueryable<ResultaatType> Where(this IQueryable<ResultaatType> resultaten, GetAllResultaatTypenFilter filter)
    {
        Guid zaakTypeId = default;
        if (filter.ZaakType != null)
        {
            zaakTypeId = Guid.Parse(filter.ZaakType.Split('/').Last());
        }

        return resultaten
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

class GetAllResultaatTypenQuery : IRequest<QueryResult<PagedResult<ResultaatType>>>
{
    public GetAllResultaatTypenFilter GetAllResultaatTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
