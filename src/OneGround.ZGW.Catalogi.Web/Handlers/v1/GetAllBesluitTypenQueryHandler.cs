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
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Extensions;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Authorization;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Handlers.v1;

class GetAllBesluitTypenQueryHandler
    : CatalogiBaseHandler<GetAllBesluitTypenQueryHandler>,
        IRequestHandler<GetAllBesluitTypenQuery, QueryResult<PagedResult<BesluitType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllBesluitTypenQueryHandler(
        ILogger<GetAllBesluitTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<BesluitType>>> Handle(GetAllBesluitTypenQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Get all BesluitTypen....");

        var filter = GetBesluitTypenFilterPredicateAsync(request.GetAllBesluitTypenFilter, request.SupportsDatumGeldigheid);

        var rsinFilter = GetRsinFilterPredicate<BesluitType>(b => b.Catalogus.Owner == _rsin);

        var query = _context.BesluitTypen.AsNoTracking().Where(rsinFilter).Where(filter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(z => z.Catalogus)
            .Include(z => z.BesluitTypeInformatieObjectTypen)
            .Where(rsinFilter)
            .OrderBy(z => z.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var datumGeldigheid = request.GetAllBesluitTypenFilter.DatumGeldigheid.GetValueOrDefault(DateOnly.FromDateTime(DateTime.Today));

        // Resolve soft BesluitType-Zaaktype relations with the current version within geldigheid
        await ResolveBesluitTypeZaakTypeRelations(pagedResult, datumGeldigheid, request.GetAllBesluitTypenFilter.Status, cancellationToken);

        // Resolve soft BesluitType-InformatieObject relations with the current version within geldigheid
        await ResolveBesluitTypeInformatieObjectTypeRelations(rsinFilter, pagedResult, datumGeldigheid, cancellationToken);

        var result = new PagedResult<BesluitType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<BesluitType>>(result, QueryStatus.OK);
    }

    private async Task ResolveBesluitTypeZaakTypeRelations(
        List<BesluitType> pagedResult,
        DateOnly datumGeldigheid,
        ConceptStatus status,
        CancellationToken cancellationToken
    )
    {
        var rsinZaakTypeFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);
        var zaaktypeBesluitTypenLookup = (
            await _context
                .ZaakTypen.AsNoTracking()
                .Where(rsinZaakTypeFilter)
                .Join(_context.ZaakTypeBesluitTypen, o => o.Id, i => i.ZaakTypeId, (z, a) => new { ZaakType = z, a.BesluitTypeOmschrijving })
                .Join(
                    _context.BesluitTypen.Where(b => status != ConceptStatus.definitief || !b.Concept),
                    k => k.BesluitTypeOmschrijving,
                    i => i.Omschrijving,
                    (z, b) => new { ZaakType = z, BesluitType = b }
                )
                .Where(z => z.ZaakType.ZaakType.CatalogusId == z.BesluitType.CatalogusId)
                .Where(z => !z.ZaakType.ZaakType.Concept)
                .Where(z =>
                    datumGeldigheid >= z.ZaakType.ZaakType.BeginGeldigheid
                    && (z.ZaakType.ZaakType.EindeGeldigheid == null || datumGeldigheid <= z.ZaakType.ZaakType.EindeGeldigheid)
                )
                .Select(z => new
                {
                    BesluitTypeId = z.BesluitType.Id,
                    Zaaktype = z.ZaakType.ZaakType,
                    BesluitType_Omschrijving = z.BesluitType.Omschrijving,
                })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.BesluitTypeId, v => v.Zaaktype);

        // For each besluittype: Set besluittype-zaaktypen [1:N] soft relations matched on zaaktype.identificatie within zaaktype.geldigheid
        foreach (var besluittype in pagedResult)
        {
            List<BesluitTypeZaakType> newBesluitTypeZaakypen = [];

            if (zaaktypeBesluitTypenLookup.Contains(besluittype.Id))
            {
                // Set soft relation between besluittype and zaaktype (is matched on omschrijving within geldigheid)
                var zaakTypenWithinGeldigheid = zaaktypeBesluitTypenLookup[besluittype.Id];

                newBesluitTypeZaakypen.AddRangeUnique(
                    zaakTypenWithinGeldigheid.Select(z => new BesluitTypeZaakType { ZaakType = z, BesluitType = besluittype }),
                    (x, y) => x.ZaakType.Url == y.ZaakType.Url
                );
            }

            // Re-map only the valid BesluitTypeZaakTypen within geldigheid!!
            besluittype.BesluitTypeZaakTypen = newBesluitTypeZaakypen;
        }
    }

    private async Task ResolveBesluitTypeInformatieObjectTypeRelations(
        Expression<Func<BesluitType, bool>> rsinFilter,
        List<BesluitType> pagedResult,
        DateOnly datumGeldigheid,
        CancellationToken cancellationToken
    )
    {
        // Build lookup besluittype-informatieobjecttypen [M:N] soft JOIN between informatieobjecttype.identificatie within informatieobjecttype.geldigheid
        var besluitTypeInformatieObjectTypenLookup = (
            await _context
                .BesluitTypen.AsNoTracking()
                .Where(rsinFilter)
                .Join(
                    _context.BesluitTypeInformatieObjectTypen,
                    o => o.Id,
                    i => i.BesluitTypeId,
                    (b, a) => new { BesluitType = b, a.InformatieObjectTypeOmschrijving }
                )
                .Join(
                    _context.InformatieObjectTypen.Where(i => !i.Concept),
                    k => k.InformatieObjectTypeOmschrijving,
                    i => i.Omschrijving,
                    (b, z) => new { BesluitType = b, InformatieObjectType = z }
                )
                .Where(b => b.BesluitType.BesluitType.CatalogusId == b.InformatieObjectType.CatalogusId)
                .Where(b =>
                    datumGeldigheid >= b.InformatieObjectType.BeginGeldigheid
                    && (b.InformatieObjectType.EindeGeldigheid == null || datumGeldigheid <= b.InformatieObjectType.EindeGeldigheid)
                )
                .Select(k => new { BesluitTypeId = k.BesluitType.BesluitType.Id, k.InformatieObjectType })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.BesluitTypeId, v => v.InformatieObjectType);

        // For each besluittype: Set besluittype-informatieobjecttypen [1:N] soft relations matched on informatieobjecttype.identificatie within informatieobjecttype.geldigheid
        foreach (var besluittype in pagedResult)
        {
            List<BesluitTypeInformatieObjectType> newBesluitTypeInformatieObjectTypen = [];

            foreach (var besluitTypeInformatieObjectTypeType in besluittype.BesluitTypeInformatieObjectTypen)
            {
                if (besluitTypeInformatieObjectTypenLookup.Contains(besluittype.Id))
                {
                    // Set soft relation between besluittype and informatieobjecttype (is matched on identifictie within geldigheid)
                    var informatieObjectTypenWithinGeldigheid = besluitTypeInformatieObjectTypenLookup[besluittype.Id]
                        .Where(z => z.Omschrijving == besluitTypeInformatieObjectTypeType.InformatieObjectTypeOmschrijving);

                    newBesluitTypeInformatieObjectTypen.AddRangeUnique(
                        informatieObjectTypenWithinGeldigheid.Select(i => new BesluitTypeInformatieObjectType { InformatieObjectType = i }),
                        (x, y) => x.InformatieObjectType.Url == y.InformatieObjectType.Url
                    );
                }
            }
            // Re-map only the valid BesluitTypeInformatieObjectTypen within geldigheid!!
            besluittype.BesluitTypeInformatieObjectTypen = newBesluitTypeInformatieObjectTypen;
        }
    }

    private Expression<Func<BesluitType, bool>> GetBesluitTypenFilterPredicateAsync(
        GetAllBesluitTypenFilter filter,
        bool supportsDatumGeldigheid = false
    )
    {
        // The reversed ZaakType-BesluitTypen must be constructed first to do filtering possible
        var filteredZaakTypeBesluitTypen =
            filter.ZaakType != null
                ? _context
                    .ZaakTypen.AsNoTracking()
                    .Where(GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin))
                    .Join(_context.ZaakTypeBesluitTypen, o => o.Id, i => i.ZaakTypeId, (z, a) => new { ZaakType = z, a.BesluitTypeOmschrijving })
                    .Join(_context.BesluitTypen, o => o.BesluitTypeOmschrijving, i => i.Omschrijving, (z, b) => new { ZaakType = z, BesluitType = b })
                    .Where(z => z.ZaakType.ZaakType.Id == _uriService.GetId(filter.ZaakType))
                    .Select(z => z.BesluitType.Id)
                    .ToList()
                : [];

        // Note: We cannot directly query on the uri for both ZaakTypen and InformatieObjectTypen so we do a lookup first (because those are mapped on identificatie and omschrijving respectively)
        var filteredInformatieObjectTypeLookup =
            filter.InformatieObjectType != null
                ? _context
                    .InformatieObjectTypen.Where(GetRsinFilterPredicate<InformatieObjectType>(z => z.Catalogus.Owner == _rsin))
                    .SingleOrDefault(z => z.Id == _uriService.GetId(filter.InformatieObjectType))
                : null;

        return z =>
            (
                filter.Status == ConceptStatus.concept && z.Concept == true
                || filter.Status == ConceptStatus.definitief && z.Concept == false
                || filter.Status == ConceptStatus.alles
            )
            && (filter.Catalogus == null || z.Catalogus.Id == _uriService.GetId(filter.Catalogus))
            && (filter.ZaakType == null || filteredZaakTypeBesluitTypen.Contains(z.Id))
            && (
                filter.InformatieObjectType == null
                || z.BesluitTypeInformatieObjectTypen.Any(z =>
                    filteredInformatieObjectTypeLookup != null
                    && z.InformatieObjectTypeOmschrijving == filteredInformatieObjectTypeLookup.Omschrijving
                )
            )
            && (
                !supportsDatumGeldigheid
                || !filter.DatumGeldigheid.HasValue
                || filter.DatumGeldigheid.Value >= z.BeginGeldigheid && !z.EindeGeldigheid.HasValue
                || filter.DatumGeldigheid.Value >= z.BeginGeldigheid && filter.DatumGeldigheid.Value <= z.EindeGeldigheid.Value
            );
    }
}

class GetAllBesluitTypenQuery : IRequest<QueryResult<PagedResult<BesluitType>>>
{
    public GetAllBesluitTypenFilter GetAllBesluitTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
    public bool SupportsDatumGeldigheid { get; internal set; }
}
