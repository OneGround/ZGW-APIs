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
using Roxit.ZGW.Common.Handlers;
using Roxit.ZGW.Common.Web.Authorization;
using Roxit.ZGW.Common.Web.Models;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;

namespace Roxit.ZGW.Catalogi.Web.Handlers.v1._3;

class GetAllZaakTypenQueryHandler
    : CatalogiBaseHandler<GetAllZaakTypenQueryHandler>,
        IRequestHandler<GetAllZaakTypenQuery, QueryResult<PagedResult<ZaakType>>>
{
    private readonly ZtcDbContext _context;

    public GetAllZaakTypenQueryHandler(
        ILogger<GetAllZaakTypenQueryHandler> logger,
        IConfiguration configuration,
        IEntityUriService uriService,
        ZtcDbContext context,
        IAuthorizationContextAccessor authorizationContextAccessor
    )
        : base(logger, configuration, uriService, authorizationContextAccessor)
    {
        _context = context;
    }

    public async Task<QueryResult<PagedResult<ZaakType>>> Handle(GetAllZaakTypenQuery request, CancellationToken cancellationToken)
    {
        var rsinFilter = GetRsinFilterPredicate<ZaakType>(b => b.Catalogus.Owner == _rsin);

        var query = _context.ZaakTypen.AsNoTracking().Where(rsinFilter).Where(request.GetAllZaakTypenFilter);

        int totalCount = await query.CountAsync(cancellationToken);

        var pagedResult = await query
            .AsSplitQuery()
            .Include(z => z.Catalogus)
            .Include(z => z.ZaakObjectTypen)
            .Include(z => z.StatusTypen)
            .Include(z => z.RolTypen)
            .Include(z => z.ResultaatTypen)
            .Include(z => z.Eigenschappen)
            .Include(z => z.ReferentieProces)
            .Include(c => c.ZaakTypeInformatieObjectTypen)
            .Include(c => c.ZaakTypeDeelZaakTypen)
            .Include(c => c.ZaakTypeGerelateerdeZaakTypen)
            .Include(z => z.ZaakTypeBesluitTypen)
            .OrderBy(z => z.CreationTime)
            .Skip(request.Pagination.Size * (request.Pagination.Page - 1))
            .Take(request.Pagination.Size)
            .ToListAsync(cancellationToken);

        var datumGeldigheid = request.GetAllZaakTypenFilter.DatumGeldigheid.GetValueOrDefault(DateOnly.FromDateTime(DateTime.Today));

        // Resolve soft Zaaktype-InformatieObjectType relations with the current version within geldigheid
        await ResolveZaakTypeInformatieObjectTypenRelations(rsinFilter, pagedResult, datumGeldigheid, cancellationToken);

        // Resolve soft Zaaktype-DeelzaakType relations with the current version within geldigheid
        await ResolveZaakTypeDeelZaakTypeRelations(rsinFilter, pagedResult, datumGeldigheid, cancellationToken);

        // Resolve soft Zaaktype-gerelateerdezaakType relations with the current version within geldigheid
        await ResolveZaakTypeGerelateerdezaakTypeRelations(rsinFilter, pagedResult, datumGeldigheid, cancellationToken);

        // Resolve soft Zaaktype-BesluitType relations with the current version within geldigheid
        await ResolveZaakTypeBesluitTypeRelations(rsinFilter, pagedResult, datumGeldigheid, cancellationToken);

        var result = new PagedResult<ZaakType> { PageResult = pagedResult, Count = totalCount };

        return new QueryResult<PagedResult<ZaakType>>(result, QueryStatus.OK);
    }

    private async Task ResolveZaakTypeInformatieObjectTypenRelations(
        Expression<Func<ZaakType, bool>> rsinFilter,
        List<ZaakType> pagedResult,
        DateOnly datumGeldigheid,
        CancellationToken cancellationToken
    )
    {
        // Build lookup zaaktype-informatieobjecttypen [M:N] soft JOIN between informatiebject.omschrijving within informatieobjecttype.geldigheid
        var zaaktypeInformatieObjectTypenLookup = (
            await _context
                .ZaakTypen.AsNoTracking()
                .Where(rsinFilter)
                .Join(
                    _context.ZaakTypeInformatieObjectTypen,
                    o => o.Id,
                    i => i.ZaakTypeId,
                    (z, a) => new { ZaakType = z, a.InformatieObjectTypeOmschrijving }
                )
                .Join(
                    _context.InformatieObjectTypen.Where(i => !i.Concept),
                    k => k.InformatieObjectTypeOmschrijving,
                    i => i.Omschrijving,
                    (z, b) => new { ZaakType = z, InformatieObjectType = b }
                )
                .Where(b => b.ZaakType.ZaakType.CatalogusId == b.InformatieObjectType.CatalogusId)
                .Where(b =>
                    datumGeldigheid >= b.InformatieObjectType.BeginGeldigheid
                    && (b.InformatieObjectType.EindeGeldigheid == null || datumGeldigheid <= b.InformatieObjectType.EindeGeldigheid)
                )
                .Select(k => new { ZaaktypeId = k.ZaakType.ZaakType.Id, k.InformatieObjectType })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.ZaaktypeId, v => v.InformatieObjectType);

        // For each zaaktype: Set zaaktype-besluittypen [1:N] soft relations matched on besluittype.omschrijving within besluittype.geldigheid
        foreach (var zaaktype in pagedResult)
        {
            List<ZaakTypeInformatieObjectType> newZaakTypeInformatieObjectTypen = [];

            foreach (var zaaktypeBesluittype in zaaktype.ZaakTypeInformatieObjectTypen)
            {
                if (zaaktypeInformatieObjectTypenLookup.Contains(zaaktype.Id))
                {
                    // Set soft relation between zaaktype and besluittype (is matched on omschrijving within geldigheid)
                    var besluitTypenWithinGeldigheid = zaaktypeInformatieObjectTypenLookup[zaaktype.Id]
                        .Where(b => b.Omschrijving == zaaktypeBesluittype.InformatieObjectTypeOmschrijving);

                    newZaakTypeInformatieObjectTypen.AddRangeUnique(
                        besluitTypenWithinGeldigheid.Select(b => new ZaakTypeInformatieObjectType { InformatieObjectType = b }),
                        (x, y) => x.InformatieObjectType.Url == y.InformatieObjectType.Url
                    );
                }
            }
            // Re-map only the valid ZaakTypeInformatieObjectTypen within geldigheid!!
            zaaktype.ZaakTypeInformatieObjectTypen = newZaakTypeInformatieObjectTypen;
        }
    }

    private async Task ResolveZaakTypeBesluitTypeRelations(
        Expression<Func<ZaakType, bool>> rsinFilter,
        List<ZaakType> pagedResult,
        DateOnly datumGeldigheid,
        CancellationToken cancellationToken
    )
    {
        // Build lookup zaaktype-besluittypen [M:N] soft JOIN between beluittype.omschrijving within besluittype.geldigheid
        var zaaktypeBesluitTypenLookup = (
            await _context
                .ZaakTypen.AsNoTracking()
                .Where(rsinFilter)
                .Join(_context.ZaakTypeBesluitTypen, o => o.Id, i => i.ZaakTypeId, (z, a) => new { ZaakType = z, a.BesluitTypeOmschrijving })
                .Join(
                    _context.BesluitTypen.Where(b => !b.Concept),
                    k => k.BesluitTypeOmschrijving,
                    i => i.Omschrijving,
                    (z, b) => new { ZaakType = z, BesluitType = b }
                )
                .Where(b => b.ZaakType.ZaakType.CatalogusId == b.BesluitType.CatalogusId)
                .Where(b =>
                    datumGeldigheid >= b.BesluitType.BeginGeldigheid
                    && (b.BesluitType.EindeGeldigheid == null || datumGeldigheid <= b.BesluitType.EindeGeldigheid)
                )
                .Select(k => new { ZaaktypeId = k.ZaakType.ZaakType.Id, k.BesluitType })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.ZaaktypeId, v => v.BesluitType);

        // For each zaaktype: Set zaaktype-besluittypen [1:N] soft relations matched on besluittype.omschrijving within besluittype.geldigheid
        foreach (var zaaktype in pagedResult)
        {
            List<ZaakTypeBesluitType> newZaakTypeBesluitTypen = [];

            foreach (var zaaktypeBesluittype in zaaktype.ZaakTypeBesluitTypen)
            {
                if (zaaktypeBesluitTypenLookup.Contains(zaaktype.Id))
                {
                    // Set soft relation between zaaktype and besluittype (is matched on omschrijving within geldigheid)
                    var besluitTypenWithinGeldigheid = zaaktypeBesluitTypenLookup[zaaktype.Id]
                        .Where(b => b.Omschrijving == zaaktypeBesluittype.BesluitTypeOmschrijving);

                    newZaakTypeBesluitTypen.AddRangeUnique(
                        besluitTypenWithinGeldigheid.Select(b => new ZaakTypeBesluitType { BesluitType = b }),
                        (x, y) => x.BesluitType.Url == y.BesluitType.Url
                    );
                }
            }
            // Re-map only the valid ZaakTypeBesluitTypen within geldigheid!!
            zaaktype.ZaakTypeBesluitTypen = newZaakTypeBesluitTypen;
        }
    }

    private async Task ResolveZaakTypeGerelateerdezaakTypeRelations(
        Expression<Func<ZaakType, bool>> rsinFilter,
        List<ZaakType> pagedResult,
        DateOnly datumGeldigheid,
        CancellationToken cancellationToken
    )
    {
        // Build lookup zaaktype-deelzaaktypen [M:N] soft JOIN between deelzaaktype.identificatie within zaaktype.geldigheid

        var zaaktypeGerelateerdeZaaktypenLookup = (
            await _context
                .ZaakTypen.AsNoTracking()
                .Where(rsinFilter)
                .Join(
                    _context.ZaakTypeGerelateerdeZaakTypen,
                    o => o.Id,
                    i => i.ZaakTypeId,
                    (z, a) => new { ZaakType = z, a.GerelateerdeZaakTypeIdentificatie }
                )
                .Join(
                    _context.ZaakTypen.Where(z => !z.Concept),
                    k => k.GerelateerdeZaakTypeIdentificatie,
                    i => i.Identificatie,
                    (z, b) => new { ZaakType = z, GerelateerdeZaakType = b }
                )
                .Where(b => b.ZaakType.ZaakType.CatalogusId == b.GerelateerdeZaakType.CatalogusId)
                .Where(b =>
                    datumGeldigheid >= b.GerelateerdeZaakType.BeginGeldigheid
                    && (b.GerelateerdeZaakType.EindeGeldigheid == null || datumGeldigheid <= b.GerelateerdeZaakType.EindeGeldigheid)
                )
                .Select(k => new { ZaaktypeId = k.ZaakType.ZaakType.Id, k.GerelateerdeZaakType })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.ZaaktypeId, v => v.GerelateerdeZaakType);

        // For each zaaktype: Set zaaktype-gerelateerdezaaktypen [1:N] soft relations matched on gerelateerdezaaktype.identificatie within zaaktype.geldigheid
        foreach (var zaaktype in pagedResult)
        {
            List<ZaakTypeGerelateerdeZaakType> newZaaktypeGerelateerdezaakTypen = [];

            foreach (var zaaktypeGerelateerdeZaakType in zaaktype.ZaakTypeGerelateerdeZaakTypen)
            {
                if (zaaktypeGerelateerdeZaaktypenLookup.Contains(zaaktype.Id))
                {
                    // Set soft relation between zaaktype and gerelateerdezaaktype (is matched on identificatie within geldigheid)
                    var gerelateerdeZaakTypenWithinGeldigheid = zaaktypeGerelateerdeZaaktypenLookup[zaaktype.Id]
                        .Where(b => b.Identificatie == zaaktypeGerelateerdeZaakType.GerelateerdeZaakTypeIdentificatie);

                    newZaaktypeGerelateerdezaakTypen.AddRangeUnique(
                        gerelateerdeZaakTypenWithinGeldigheid.Select(z => new ZaakTypeGerelateerdeZaakType
                        {
                            GerelateerdeZaakType = z,
                            AardRelatie = zaaktypeGerelateerdeZaakType.AardRelatie,
                            Toelichting = zaaktypeGerelateerdeZaakType.Toelichting,
                        }),
                        (x, y) => x.GerelateerdeZaakType.Url == y.GerelateerdeZaakType.Url
                    );
                }
            }
            // Re-map only the valid ZaakTypeGerelateerdeZaakTypen within geldigheid!!
            zaaktype.ZaakTypeGerelateerdeZaakTypen = newZaaktypeGerelateerdezaakTypen;
        }
    }

    private async Task ResolveZaakTypeDeelZaakTypeRelations(
        Expression<Func<ZaakType, bool>> rsinFilter,
        List<ZaakType> pagedResult,
        DateOnly datumGeldigheid,
        CancellationToken cancellationToken
    )
    {
        // Build lookup zaaktype-deelzaaktypen [M:N] soft JOIN between deelzaaktype.identificatie within zaaktype.geldigheid

        var zaaktypeDeelZaaktypenLookup = (
            await _context
                .ZaakTypen.AsNoTracking()
                .Where(rsinFilter)
                .Join(_context.ZaakTypeDeelZaakTypen, o => o.Id, i => i.ZaakTypeId, (z, a) => new { ZaakType = z, a.DeelZaakTypeIdentificatie })
                .Join(
                    _context.ZaakTypen.Where(z => !z.Concept),
                    k => k.DeelZaakTypeIdentificatie,
                    i => i.Identificatie,
                    (z, b) => new { ZaakType = z, DeelZaakType = b }
                )
                .Where(b => b.ZaakType.ZaakType.CatalogusId == b.DeelZaakType.CatalogusId)
                .Where(b =>
                    datumGeldigheid >= b.DeelZaakType.BeginGeldigheid
                    && (b.DeelZaakType.EindeGeldigheid == null || datumGeldigheid <= b.DeelZaakType.EindeGeldigheid)
                )
                .Select(k => new { ZaaktypeId = k.ZaakType.ZaakType.Id, k.DeelZaakType })
                .ToListAsync(cancellationToken)
        ).ToLookup(k => k.ZaaktypeId, v => v.DeelZaakType);

        // For each zaaktype: Set zaaktype-deelzaaktypen [1:N] soft relations matched on deelzaaktype.identificatie within zaaktype.geldigheid
        foreach (var zaaktype in pagedResult)
        {
            List<ZaakTypeDeelZaakType> newZaaktypeDeelzaakTypen = [];

            foreach (var zaaktypeDeelzaakType in zaaktype.ZaakTypeDeelZaakTypen)
            {
                if (zaaktypeDeelZaaktypenLookup.Contains(zaaktype.Id))
                {
                    // Set soft relation between zaaktype and deelzaaktype (is matched on identificatie within geldigheid)
                    var deelZaakTypenWithinGeldigheid = zaaktypeDeelZaaktypenLookup[zaaktype.Id]
                        .Where(b => b.Identificatie == zaaktypeDeelzaakType.DeelZaakTypeIdentificatie);

                    newZaaktypeDeelzaakTypen.AddRangeUnique(
                        deelZaakTypenWithinGeldigheid.Select(z => new ZaakTypeDeelZaakType { DeelZaakType = z }),
                        (x, y) => x.DeelZaakType.Url == y.DeelZaakType.Url
                    );
                }
            }
            // Re-map only the valid ZaakTypeDeelZaakTypen within geldigheid!!
            zaaktype.ZaakTypeDeelZaakTypen = newZaaktypeDeelzaakTypen;
        }
    }
}

internal static class IQueryableExtension
{
    public static IQueryable<ZaakType> Where(this IQueryable<ZaakType> zaaktypen, Models.v1.GetAllZaakTypenFilter filter)
    {
        Guid catalogusId = default;
        if (filter.Catalogus != null)
        {
            catalogusId = Guid.Parse(filter.Catalogus.Split('/').Last());
        }

        return zaaktypen
            .WhereIf(
                filter.Status != ConceptStatus.alles,
                z => filter.Status == ConceptStatus.concept && z.Concept == true || filter.Status == ConceptStatus.definitief && z.Concept == false
            )
            .WhereIf(filter.Catalogus != null, z => z.Catalogus.Id == catalogusId)
            .WhereIf(filter.Identificatie != null, z => z.Identificatie == filter.Identificatie)
            .WhereIf(filter.Trefwoorden.Length != 0, z => z.Trefwoorden.Any(x => filter.Trefwoorden.Any(y => y == x)))
            .WhereIf(
                filter.DatumGeldigheid.HasValue,
                z =>
                    filter.DatumGeldigheid.Value >= z.BeginGeldigheid && !z.EindeGeldigheid.HasValue
                    || filter.DatumGeldigheid.Value >= z.BeginGeldigheid && filter.DatumGeldigheid.Value <= z.EindeGeldigheid.Value
            );
    }
}

class GetAllZaakTypenQuery : IRequest<QueryResult<PagedResult<ZaakType>>>
{
    public Models.v1.GetAllZaakTypenFilter GetAllZaakTypenFilter { get; internal set; }
    public PaginationFilter Pagination { get; internal set; }
}
